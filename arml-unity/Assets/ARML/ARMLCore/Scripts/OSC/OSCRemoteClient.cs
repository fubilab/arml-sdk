using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>A server found by discovery (or entered manually).</summary>
public sealed class OscServerInfo
{
    public IPEndPoint Endpoint { get; }
    public string Name { get; internal set; }
    /// <summary>Time.unscaledTime of the last discovery reply.</summary>
    public float LastSeen { get; internal set; }

    internal OscServerInfo(IPEndPoint endpoint, string name, float lastSeen)
    {
        Endpoint = endpoint;
        Name = name;
        LastSeen = lastSeen;
    }
}

/// <summary>
/// Phone-side OSC client. Finds the editor/Linux builds by UDP broadcast (or a manual IP),
/// subscribes to one of them and mirrors the parameter schema it advertises. The client has no
/// built-in knowledge of any parameter: whatever the server registers shows up in <see cref="Params"/>.
///
/// Discovery keeps running while connected, so <see cref="Servers"/> lists every joinable server
/// on the network and <see cref="Select"/> switches between them.
///
/// One UDP socket is used for both sending and receiving, so the server's replies come
/// back to the same port it heard us on.
/// </summary>
public class OSCRemoteClient : MonoBehaviour
{
    public enum ConnectionState { Searching, Connecting, Connected }

    [Header("Network")]
    [SerializeField] private int discoveryPort = 8888;
    [SerializeField] private int defaultOscPort = 9000;
    [SerializeField] private float discoveryInterval = 1f;
    [SerializeField] private float sendInterval = 0.1f;
    [SerializeField] private float pingInterval = 3f;
    [SerializeField] private float pongTimeout = 8f;

    [Header("Multiple servers")]
    [SerializeField, Tooltip("Join the first server found while searching. Turn off to always pick from the list.")]
    private bool autoConnect = true;
    [SerializeField, Tooltip("Seconds between discovery probes once a server is chosen (keeps the server list fresh)")]
    private float connectedDiscoveryInterval = 3f;
    [SerializeField, Tooltip("Seconds without a discovery reply before a server is dropped from the list")]
    private float serverTimeout = 10f;

    public ConnectionState State { get; private set; } = ConnectionState.Searching;
    public string ServerName { get; private set; } = "";
    /// <summary>Set when the sockets could not be opened (e.g. missing INTERNET permission on Android).</summary>
    public string Error { get; private set; }
    /// <summary>This device's LAN address as seen by the OS (null if unknown).</summary>
    public IPAddress LocalAddress { get; private set; }
    public string DebugInfo { get; private set; } = "";
    public int ProbesSent { get; private set; }
    public int RepliesSeen { get; private set; }
    public IPEndPoint Server { get; private set; }

    /// <summary>Parameters advertised by the server, in display order.</summary>
    public IReadOnlyList<OscParamInfo> Params => _params;

    /// <summary>Raised on the main thread when the server sent a new parameter layout.</summary>
    public event Action SchemaChanged;
    /// <summary>Raised on the main thread after values were updated from the server.</summary>
    public event Action StateChanged;
    /// <summary>Raised when the connection state or the selected server changes.</summary>
    public event Action ConnectionChanged;
    /// <summary>Raised when a server appears, disappears or is renamed.</summary>
    public event Action ServersChanged;

    /// <summary>Servers currently answering discovery, sorted by name. The connected one is always listed.</summary>
    public IReadOnlyList<OscServerInfo> Servers => _servers;

    private readonly List<OscServerInfo> _servers = new List<OscServerInfo>();
    private readonly List<OscParamInfo> _params = new List<OscParamInfo>();
    private readonly Dictionary<string, OscParamInfo> _byAddress = new Dictionary<string, OscParamInfo>();

    // Schema being received (it may span several datagrams).
    private readonly List<OscParamInfo> _building = new List<OscParamInfo>();
    private int _buildRevision = -1;
    private int _expected = -1;
    private int _completedRevision = -1;

    private const string DiscoveryRequest = "DISCOVER_OSC_SERVER";
    private const string DiscoveryReplyPrefix = "OSC_SERVER_HERE:";

    private UdpClient _oscSocket;
    private UdpClient _discoverySocket;
    private Thread _oscThread;
    private Thread _discoveryThread;
    private volatile bool _running;

    private readonly ConcurrentQueue<OscMessage> _inbox = new ConcurrentQueue<OscMessage>();
    private readonly ConcurrentQueue<(IPAddress ip, int port, string name)> _found =
        new ConcurrentQueue<(IPAddress, int, string)>();

    private readonly Dictionary<string, byte[]> _pendingOut = new Dictionary<string, byte[]>();
    private List<IPEndPoint> _broadcastTargets = new List<IPEndPoint>();
    private List<IPEndPoint> _sweepTargets = new List<IPEndPoint>();

    private float _lastDiscovery = -100f;
    private float _lastFlush;
    private float _lastHello = -100f;
    private float _lastPing;
    private float _lastPong;

    // ---------------------------------------------------------------- lifecycle

    private void OnEnable()
    {
        try
        {
            _oscSocket = new UdpClient(AddressFamily.InterNetwork);
            _oscSocket.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            _discoverySocket = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
            _discoverySocket.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        }
        catch (Exception e)
        {
            Error = e.Message;
            Debug.LogError($"[OSC Client] Failed to open sockets: {e.Message}");
            CloseSockets();
            return;
        }

        _broadcastTargets = BuildBroadcastTargets();
        LocalAddress = GetLocalAddress();
        _sweepTargets = BuildSweepTargets(LocalAddress);
        ProbesSent = 0;
        RepliesSeen = 0;
        _running = true;
        _oscThread = StartThread(OscLoop, "OSC Client Receive");
        _discoveryThread = StartThread(DiscoveryLoop, "OSC Client Discovery");
        SetState(ConnectionState.Searching);
    }

    private void OnDisable()
    {
        _running = false;
        CloseSockets();
        _oscThread?.Join(500);
        _discoveryThread?.Join(500);
        _oscThread = null;
        _discoveryThread = null;
    }

    private void Update()
    {
        if (!_running) return;

        // Discovery replies.
        bool serversChanged = false;
        while (_found.TryDequeue(out var found))
        {
            RepliesSeen++;
            var endpoint = new IPEndPoint(found.ip, found.port);
            serversChanged |= UpsertServer(endpoint, found.name);
            if (State == ConnectionState.Searching && autoConnect)
                Connect(endpoint, found.name);
        }
        serversChanged |= ExpireServers();
        if (serversChanged) ServersChanged?.Invoke();

        // Messages from the server. Anything from another address (e.g. the server we just left,
        // which keeps pushing until it times us out) must not touch the current mirror.
        bool changed = false;
        while (_inbox.TryDequeue(out OscMessage msg))
        {
            if (Server == null || (msg.Remote != null && !msg.Remote.Equals(Server))) continue;
            changed |= Apply(msg);
        }
        if (changed) StateChanged?.Invoke();

        // Keep probing even when connected so other servers show up in (and drop out of) the list.
        float probeInterval = State == ConnectionState.Searching ? discoveryInterval : connectedDiscoveryInterval;
        if (Time.unscaledTime - _lastDiscovery >= probeInterval) Broadcast();

        switch (State)
        {
            case ConnectionState.Connecting:
                if (Time.unscaledTime - _lastHello >= 1f) SendHello();
                break;

            case ConnectionState.Connected:
                if (Time.unscaledTime - _lastPing >= pingInterval)
                {
                    _lastPing = Time.unscaledTime;
                    SendNow(OscCodec.EncodeMessage(OscAddresses.Ping));
                }
                if (Time.unscaledTime - _lastPong > pongTimeout)
                {
                    // Server went away (or restarted and forgot us): say hello again.
                    SetState(ConnectionState.Connecting);
                }
                break;
        }

        FlushPending(false);
    }

    // ---------------------------------------------------------------- connection

    /// <summary>Connect to a known address, skipping discovery.</summary>
    public bool ConnectManual(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;

        string address = host.Trim();
        int port = defaultOscPort;
        int colon = address.LastIndexOf(':');
        if (colon > 0 && int.TryParse(address.Substring(colon + 1), out int parsed))
        {
            port = parsed;
            address = address.Substring(0, colon);
        }

        if (!IPAddress.TryParse(address, out IPAddress ip)) return false;

        var endpoint = new IPEndPoint(ip, port);
        // A manually entered server shows up in the list too (named after its address until discovery names it).
        if (UpsertServer(endpoint, address)) ServersChanged?.Invoke();
        Connect(endpoint, FindServer(endpoint)?.Name ?? address);
        return true;
    }

    /// <summary>Switch to one of the servers in <see cref="Servers"/>.</summary>
    public void Select(OscServerInfo server)
    {
        if (server == null) return;
        Connect(server.Endpoint, server.Name);
    }

    /// <summary>Leave the current server, forget all known servers and search again.</summary>
    public void Search()
    {
        LeaveServer();
        ServerName = "";
        _lastDiscovery = -100f;
        if (_servers.Count > 0)
        {
            _servers.Clear();
            ServersChanged?.Invoke();
        }
        SetState(ConnectionState.Searching);
    }

    // Unsubscribes from the current server (best effort) and drops everything mirrored from it.
    private void LeaveServer()
    {
        if (Server != null) SendTo(OscCodec.EncodeMessage(OscAddresses.Bye), Server);
        Server = null;
        _pendingOut.Clear();
        ClearSchema();
    }

    private OscServerInfo FindServer(IPEndPoint endpoint)
    {
        foreach (OscServerInfo s in _servers)
            if (s.Endpoint.Equals(endpoint)) return s;
        return null;
    }

    // Adds a server or refreshes it; returns true if the list changed (new entry or new name).
    private bool UpsertServer(IPEndPoint endpoint, string name)
    {
        OscServerInfo existing = FindServer(endpoint);
        if (existing != null)
        {
            existing.LastSeen = Time.unscaledTime;
            if (existing.Name == name) return false;

            existing.Name = name;
            SortServers();
            return true;
        }

        _servers.Add(new OscServerInfo(endpoint, name, Time.unscaledTime));
        SortServers();
        return true;
    }

    private void SortServers()
    {
        _servers.Sort((a, b) =>
        {
            int byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            return byName != 0 ? byName : string.CompareOrdinal(a.Endpoint.ToString(), b.Endpoint.ToString());
        });
    }

    // Drops servers that stopped answering. The one we're connected to stays (its own ping/pong decides).
    private bool ExpireServers()
    {
        bool removed = false;
        for (int i = _servers.Count - 1; i >= 0; i--)
        {
            OscServerInfo s = _servers[i];
            if (Time.unscaledTime - s.LastSeen <= serverTimeout) continue;
            if (Server != null && s.Endpoint.Equals(Server)) continue;

            _servers.RemoveAt(i);
            removed = true;
        }
        return removed;
    }

    private void ClearSchema()
    {
        bool had = _params.Count > 0;
        _params.Clear();
        _byAddress.Clear();
        _building.Clear();
        _buildRevision = _completedRevision = -1;
        _expected = -1;
        if (had) SchemaChanged?.Invoke();
    }

    private void Connect(IPEndPoint server, string name)
    {
        // Switching servers: say goodbye to the old one and drop its parameters before the new
        // schema arrives (re-connecting to the same server keeps its schema).
        if (Server != null && !Server.Equals(server)) LeaveServer();

        Server = server;
        ServerName = name;
        _lastHello = -100f;
        _lastPong = Time.unscaledTime;
        SetState(ConnectionState.Connecting);
    }

    private void SetState(ConnectionState state)
    {
        State = state;
        ConnectionChanged?.Invoke();
    }

    private void SendHello()
    {
        _lastHello = Time.unscaledTime;
        SendNow(OscCodec.EncodeMessage(OscAddresses.Hello));
    }

    // ---------------------------------------------------------------- sending

    /// <summary>Sends a parameter's current value (queued and coalesced per address; triggers go out immediately).</summary>
    public void SendParam(OscParamInfo param)
    {
        if (param.Type == OscParamType.Trigger)
        {
            FlushPending(true);
            SendNow(OscCodec.EncodeMessage(param.Address, param.EncodeValue()));
        }
        else
        {
            SendSetting(param.Address, param.EncodeValue());
        }
    }

    /// <summary>Queues a setting; only the newest value per address is sent, at most every sendInterval.</summary>
    public void SendSetting(string address, params object[] args)
    {
        _pendingOut[address] = OscCodec.EncodeMessage(address, args);
    }

    private void FlushPending(bool force)
    {
        if (_pendingOut.Count == 0) return;
        if (!force && Time.unscaledTime - _lastFlush < sendInterval) return;

        _lastFlush = Time.unscaledTime;
        foreach (byte[] data in _pendingOut.Values) SendNow(data);
        _pendingOut.Clear();
    }

    private void SendNow(byte[] data) => SendTo(data, Server);

    private void SendTo(byte[] data, IPEndPoint target)
    {
        var socket = _oscSocket;
        if (socket == null || target == null) return;

        try { socket.Send(data, data.Length, target); }
        catch (Exception e) { Debug.LogWarning($"[OSC Client] Send failed: {e.Message}"); }
    }

    private void Broadcast()
    {
        _lastDiscovery = Time.unscaledTime;
        var socket = _discoverySocket;
        if (socket == null) return;

        byte[] request = Encoding.UTF8.GetBytes(DiscoveryRequest);
        int sent = 0;
        foreach (IPEndPoint target in _broadcastTargets)
        {
            try { socket.Send(request, request.Length, target); sent++; }
            catch (Exception) { /* some interfaces refuse broadcast; try the rest */ }
        }

        // Many phones/routers drop broadcasts, so also probe every address on the local /24 directly.
        foreach (IPEndPoint target in _sweepTargets)
        {
            try { socket.Send(request, request.Length, target); sent++; }
            catch (Exception) { /* ignore */ }
        }

        ProbesSent += sent;
        DebugInfo = $"local {(LocalAddress != null ? LocalAddress.ToString() : "unknown")}, " +
                    $"{_broadcastTargets.Count} broadcast + {_sweepTargets.Count} unicast probes/s, " +
                    $"{ProbesSent} sent, {RepliesSeen} replies";
    }

    // The address the OS would use to reach the LAN. Connecting a UDP socket sends nothing.
    private static IPAddress GetLocalAddress()
    {
        try
        {
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                s.Connect(new IPEndPoint(IPAddress.Parse("8.8.8.8"), 65530));
                return ((IPEndPoint)s.LocalEndPoint).Address;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    private List<IPEndPoint> BuildSweepTargets(IPAddress local)
    {
        var targets = new List<IPEndPoint>();
        if (local == null) return targets;

        byte[] ip = local.GetAddressBytes();
        for (int host = 1; host < 255; host++)
        {
            if (host == ip[3]) continue;
            targets.Add(new IPEndPoint(new IPAddress(new[] { ip[0], ip[1], ip[2], (byte)host }), discoveryPort));
        }
        return targets;
    }

    private List<IPEndPoint> BuildBroadcastTargets()
    {
        var targets = new List<IPEndPoint>
        {
            new IPEndPoint(IPAddress.Broadcast, discoveryPort),
            new IPEndPoint(IPAddress.Loopback, discoveryPort) // editor on the same machine
        };

        // Some Android Wi-Fi stacks drop 255.255.255.255, so also use each subnet's broadcast address.
        try
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;

                foreach (UnicastIPAddressInformation info in nic.GetIPProperties().UnicastAddresses)
                {
                    if (info.Address.AddressFamily != AddressFamily.InterNetwork || info.IPv4Mask == null) continue;

                    byte[] ip = info.Address.GetAddressBytes();
                    byte[] mask = info.IPv4Mask.GetAddressBytes();
                    var broadcast = new byte[4];
                    for (int i = 0; i < 4; i++) broadcast[i] = (byte)(ip[i] | ~mask[i]);

                    var endpoint = new IPEndPoint(new IPAddress(broadcast), discoveryPort);
                    if (!targets.Contains(endpoint)) targets.Add(endpoint);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OSC Client] Could not enumerate network interfaces: {e.Message}");
        }

        return targets;
    }

    // ---------------------------------------------------------------- receiving

    // Applies one server message to the local mirror; returns true if values changed.
    private bool Apply(OscMessage m)
    {
        switch (m.Address)
        {
            case OscAddresses.Pong:
                _lastPong = Time.unscaledTime;
                return false;

            case OscAddresses.SchemaBegin:
                if (!m.TryGetInt(1, out int count) || !m.TryGetInt(2, out int revision)) return false;
                if (revision == _completedRevision)
                {
                    MarkConnected(); // same layout as before (re-subscribed): values follow in the param messages
                    return false;
                }
                if (revision != _buildRevision) StartBuild(revision);
                _expected = count;
                return TryCompleteBuild();

            case OscAddresses.SchemaParam:
                return ApplySchemaParam(m);
        }

        if (_byAddress.TryGetValue(m.Address, out OscParamInfo param))
            return param.ApplyValue(m);

        return false;
    }

    private void StartBuild(int revision)
    {
        _building.Clear();
        _buildRevision = revision;
        _expected = -1;
    }

    private bool ApplySchemaParam(OscMessage m)
    {
        if (!m.TryGetInt(0, out int revision)) return false;

        OscParamInfo incoming = OscParamInfo.FromSchema(m);
        if (incoming == null) return false;

        // Same layout we already have (hello was repeated, or we re-subscribed): just refresh values.
        if (revision == _completedRevision)
        {
            if (!_byAddress.TryGetValue(incoming.Address, out OscParamInfo existing)) return false;
            existing.CopyValueFrom(incoming);
            return true;
        }

        if (revision != _buildRevision) StartBuild(revision);
        if (!_building.Exists(p => p.Address == incoming.Address)) _building.Add(incoming);
        return TryCompleteBuild();
    }

    private bool TryCompleteBuild()
    {
        if (_expected < 0 || _building.Count < _expected || _buildRevision == _completedRevision) return false;

        _params.Clear();
        _byAddress.Clear();
        foreach (OscParamInfo p in _building)
        {
            _params.Add(p);
            _byAddress[p.Address] = p;
        }

        _building.Clear();
        _completedRevision = _buildRevision;
        MarkConnected();
        SchemaChanged?.Invoke();
        return true;
    }

    private void MarkConnected()
    {
        _lastPong = Time.unscaledTime;
        _lastPing = Time.unscaledTime;
        if (State != ConnectionState.Connected) SetState(ConnectionState.Connected);
    }

    // ---------------------------------------------------------------- threads

    private void CloseSockets()
    {
        try { _oscSocket?.Close(); } catch { /* ignore */ }
        try { _discoverySocket?.Close(); } catch { /* ignore */ }
        _oscSocket = null;
        _discoverySocket = null;
    }

    private static Thread StartThread(ThreadStart body, string name)
    {
        var t = new Thread(body) { IsBackground = true, Name = name };
        t.Start();
        return t;
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
                OscCodec.Parse(data, data.Length, new IPEndPoint(remote.Address, remote.Port), _inbox);
            }
            catch (SocketException) { if (!_running) break; Thread.Sleep(50); }
            catch (ObjectDisposedException) { break; }
            catch (Exception e) { Debug.LogWarning($"[OSC Client] Bad packet: {e.Message}"); }
        }
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
                string text = Encoding.UTF8.GetString(bytes);
                if (!text.StartsWith(DiscoveryReplyPrefix)) continue;

                // OSC_SERVER_HERE:{port}:{name}
                string[] parts = text.Substring(DiscoveryReplyPrefix.Length).Split(new[] { ':' }, 2);
                if (!int.TryParse(parts[0], out int port)) continue;

                _found.Enqueue((remote.Address, port, parts.Length > 1 ? parts[1] : remote.Address.ToString()));
            }
            catch (SocketException) { if (!_running) break; Thread.Sleep(50); }
            catch (ObjectDisposedException) { break; }
            catch (Exception e) { Debug.LogWarning($"[OSC Client] Discovery error: {e.Message}"); }
        }
    }
}
