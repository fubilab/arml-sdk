using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Generic OSC remote-control server. Answers UDP discovery broadcasts so the phone app can find
/// this machine, then describes every parameter registered by the scene's
/// <see cref="OscParameterSource"/> components (the "schema") and keeps phone and game in sync.
/// The phone app knows nothing about specific parameters: what is controllable is decided here.
///
/// Discovery: client broadcasts "DISCOVER_OSC_SERVER" to discoveryPort,
///            server replies "OSC_SERVER_HERE:{oscPort}:{deviceName}".
/// Session:   phone sends /osc/hello (and /osc/ping every few seconds); server answers with the
///            schema, then pushes value changes made on this side.
/// Edits:     phone sends the new value to the parameter's own address.
/// </summary>
public class OSCServerController : MonoBehaviour
{
    [Header("Network Configuration")]
    [SerializeField] private int discoveryPort = 8888;
    [SerializeField] private int oscPort = 9000;
    [SerializeField] private string deviceName = "";
    [SerializeField, Tooltip("Seconds without a hello/ping before a remote stops receiving state updates")]
    private float subscriberTimeout = 15f;
    [SerializeField, Tooltip("How often (seconds) game-side value changes are pushed to the phones")]
    private float stateInterval = 0.1f;

    [SerializeField] private bool logMessages;

    private const string DiscoveryRequest = "DISCOVER_OSC_SERVER";

    private static OSCServerController _instance;

    private UdpClient _discoverySocket;
    private UdpClient _oscSocket;
    private Thread _discoveryThread;
    private Thread _oscThread;
    private volatile bool _running;

    // Written by the OSC thread, drained on the main thread.
    private readonly ConcurrentQueue<OscMessage> _queue = new ConcurrentQueue<OscMessage>();
    // Messages applied this frame, in arrival order, newest per address only.
    private readonly List<OscMessage> _pending = new List<OscMessage>();

    private readonly OscParameterRegistry _registry = new OscParameterRegistry();
    private bool _sourcesDirty = true;
    private int _revision;

    // Remotes that said hello recently -> time of last contact.
    private readonly Dictionary<IPEndPoint, float> _subscribers = new Dictionary<IPEndPoint, float>();
    private readonly List<IPEndPoint> _expired = new List<IPEndPoint>();
    private float _nextStatePush;

    public OscParameterRegistry Parameters => _registry;

    /// <summary>Called by sources when they are enabled/disabled; the schema is rebuilt and re-sent next frame.</summary>
    public static void NotifySourcesChanged()
    {
        if (_instance != null) _instance._sourcesDirty = true;
    }

    /// <summary>Re-reads the sources (e.g. after changing a source's option list at runtime).</summary>
    public void RefreshSchema() => _sourcesDirty = true;

    private void Awake()
    {
        if (string.IsNullOrEmpty(deviceName)) deviceName = Environment.MachineName;
        // Random start so a restarted server is never mistaken for the previous session's schema.
        _revision = UnityEngine.Random.Range(1, int.MaxValue / 2);

        // Fields marked [RuntimeParameter] are exposed without any setup.
        if (FindFirstObjectByType<OSCRuntimeParameterSource>() == null)
            gameObject.AddComponent<OSCRuntimeParameterSource>();
    }

    private void OnEnable()
    {
        _instance = this;
        _sourcesDirty = true;
        _subscribers.Clear();

        try
        {
            _discoverySocket = CreateSocket(discoveryPort);
            _oscSocket = CreateSocket(oscPort);
        }
        catch (Exception e)
        {
            Debug.LogError($"[OSC] Failed to bind sockets (ports {discoveryPort}/{oscPort} in use?): {e.Message}");
            CloseSockets();
            return;
        }

        _running = true;
        _discoveryThread = StartThread(DiscoveryLoop, "OSC Discovery");
        _oscThread = StartThread(OscLoop, "OSC Receive");
        Debug.Log($"[OSC] Listening: OSC {oscPort}, discovery {discoveryPort}");
    }

    private void OnDisable()
    {
        if (_instance == this) _instance = null;

        _running = false;
        CloseSockets(); // unblocks Receive() on the worker threads
        _discoveryThread?.Join(500);
        _oscThread?.Join(500);
        _discoveryThread = null;
        _oscThread = null;
    }

    // ---------------------------------------------------------------- schema

    private void RescanSources()
    {
        _sourcesDirty = false;
        _registry.Clear();

        var sources = FindObjectsByType<OscParameterSource>(FindObjectsSortMode.InstanceID);
        foreach (OscParameterSource source in sources)
        {
            if (!source.isActiveAndEnabled) continue;

            try { source.Register(_registry); }
            catch (Exception e) { Debug.LogWarning($"[OSC] {source.name}: could not register parameters: {e.Message}"); }
        }

        // Remember the current values so only later changes are pushed.
        foreach (OscParameter p in _registry.Parameters) p.TryGetChange(out _);

        _revision++;
        Debug.Log($"[OSC] {_registry.Parameters.Count} parameters from {sources.Length} source(s)");
        if (_registry.Parameters.Count == 0)
            Debug.LogWarning("[OSC] No OscParameterSource in the scene: the phone will show nothing to control");

        // Connected phones get the new layout right away.
        foreach (IPEndPoint remote in _subscribers.Keys) SendSchema(remote);
    }

    private void SendSchema(IPEndPoint remote)
    {
        var messages = new List<byte[]>(_registry.Parameters.Count + 1)
        {
            OscCodec.EncodeMessage(OscAddresses.SchemaBegin,
                OscAddresses.ProtocolVersion, _registry.Parameters.Count, _revision)
        };

        foreach (OscParameter p in _registry.Parameters)
        {
            p.ReadValue(); // refresh Info from the game before describing it
            messages.Add(OscCodec.EncodeMessage(OscAddresses.SchemaParam, p.Info.EncodeSchema(_revision)));
        }

        foreach (byte[] datagram in OscCodec.EncodeBundles(messages)) Send(remote, datagram);
    }

    // Pushes values that changed on this side (other scripts, presets, ...) to every phone.
    private void PushChanges()
    {
        if (_subscribers.Count == 0) return;

        List<byte[]> messages = null;
        foreach (OscParameter p in _registry.Parameters)
        {
            if (!p.TryGetChange(out object[] value)) continue;
            (messages ??= new List<byte[]>()).Add(OscCodec.EncodeMessage(p.Info.Address, value));
        }

        if (messages == null) return;

        foreach (byte[] datagram in OscCodec.EncodeBundles(messages))
            foreach (IPEndPoint remote in _subscribers.Keys)
                Send(remote, datagram);
    }

    // ---------------------------------------------------------------- update

    private void Update()
    {
        if (_sourcesDirty) RescanSources();

        while (_queue.TryDequeue(out OscMessage msg))
        {
            // A newer value replaces an older one for the same address, except for events
            // (hello/ping/triggers) which must all be seen.
            if (!IsEvent(msg.Address)) _pending.RemoveAll(p => p.Address == msg.Address);
            _pending.Add(msg);
        }

        ExpireSubscribers();

        foreach (OscMessage msg in _pending)
        {
            if (logMessages) Debug.Log($"[OSC] {msg.Address} ({msg.ArgCount} args)");
            try { Dispatch(msg); }
            catch (Exception e) { Debug.LogWarning($"[OSC] {msg.Address}: {e.Message}"); }
        }
        _pending.Clear();

        if (Time.unscaledTime >= _nextStatePush)
        {
            _nextStatePush = Time.unscaledTime + stateInterval;
            PushChanges();
        }
    }

    private bool IsEvent(string address) =>
        address == OscAddresses.Hello || address == OscAddresses.Ping || address == OscAddresses.Bye ||
        (_registry.TryGet(address, out OscParameter p) && p.IsTrigger);

    private void Dispatch(OscMessage m)
    {
        switch (m.Address)
        {
            case OscAddresses.Hello:
                Subscribe(m);
                SendSchema(m.Remote);
                return;

            case OscAddresses.Ping:
                // A ping from a remote we don't know means we restarted: it needs the schema again.
                if (Subscribe(m)) SendSchema(m.Remote);
                Send(m.Remote, OscCodec.EncodeMessage(OscAddresses.Pong));
                return;

            case OscAddresses.Bye:
                // The phone switched to another server: stop pushing state to it.
                if (m.Remote != null) _subscribers.Remove(m.Remote);
                return;
        }

        if (_registry.TryGet(m.Address, out OscParameter parameter)) parameter.Apply(m);
        else if (_registry.TryGetRaw(m.Address, out var raw)) raw(m);
        else if (logMessages) Debug.LogWarning($"[OSC] Unhandled address {m.Address}");
    }

    // ---------------------------------------------------------------- subscribers

    /// <summary>Returns true if the remote was not subscribed before.</summary>
    private bool Subscribe(OscMessage m)
    {
        if (m.Remote == null) return false;

        bool isNew = !_subscribers.ContainsKey(m.Remote);
        _subscribers[m.Remote] = Time.unscaledTime;
        return isNew;
    }

    private void ExpireSubscribers()
    {
        if (_subscribers.Count == 0) return;

        _expired.Clear();
        foreach (var kv in _subscribers)
            if (Time.unscaledTime - kv.Value > subscriberTimeout) _expired.Add(kv.Key);
        foreach (IPEndPoint remote in _expired) _subscribers.Remove(remote);
    }

    private void Send(IPEndPoint remote, byte[] data)
    {
        var socket = _oscSocket;
        if (socket == null || remote == null) return;

        try { socket.Send(data, data.Length, remote); }
        catch (Exception e) { Debug.LogWarning($"[OSC] Send to {remote} failed: {e.Message}"); }
    }

    // ---------------------------------------------------------------- sockets

    private static UdpClient CreateSocket(int port)
    {
        var client = new UdpClient(AddressFamily.InterNetwork);
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        return client;
    }

    private void CloseSockets()
    {
        try { _discoverySocket?.Close(); } catch { /* ignore */ }
        try { _oscSocket?.Close(); } catch { /* ignore */ }
        _discoverySocket = null;
        _oscSocket = null;
    }

    private static Thread StartThread(ThreadStart body, string name)
    {
        var t = new Thread(body) { IsBackground = true, Name = name };
        t.Start();
        return t;
    }

    private void DiscoveryLoop()
    {
        var socket = _discoverySocket;
        var remote = new IPEndPoint(IPAddress.Any, 0);

        while (_running)
        {
            try
            {
                byte[] bytes = socket.Receive(ref remote);
                if (Encoding.UTF8.GetString(bytes).Trim() != DiscoveryRequest) continue;

                byte[] reply = Encoding.UTF8.GetBytes($"OSC_SERVER_HERE:{oscPort}:{deviceName}");
                socket.Send(reply, reply.Length, remote);
            }
            catch (SocketException) { if (!_running) break; Thread.Sleep(50); }
            catch (ObjectDisposedException) { break; }
            catch (Exception e) { Debug.LogWarning($"[OSC] Discovery error: {e.Message}"); Thread.Sleep(50); }
        }
    }

    private void OscLoop()
    {
        var socket = _oscSocket;
        var remote = new IPEndPoint(IPAddress.Any, 0);

        while (_running)
        {
            try
            {
                byte[] data = socket.Receive(ref remote);
                // 'remote' is reused by Receive, so hand out a copy.
                OscCodec.Parse(data, data.Length, new IPEndPoint(remote.Address, remote.Port), _queue);
            }
            catch (SocketException) { if (!_running) break; Thread.Sleep(50); }
            catch (ObjectDisposedException) { break; }
            catch (Exception e) { Debug.LogWarning($"[OSC] Bad packet from {remote}: {e.Message}"); }
        }
    }
}
