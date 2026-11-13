using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One parameter exposed to the remote: its description plus the getter/setter that bind it to the game.</summary>
public sealed class OscParameter
{
    public readonly OscParamInfo Info;

    private readonly Action<OscParamInfo> _read;   // fills Info's value from the game
    private readonly Action<OscParamInfo> _write;  // pushes Info's value into the game
    private object[] _lastSent;

    public OscParameter(OscParamInfo info, Action<OscParamInfo> read, Action<OscParamInfo> write)
    {
        Info = info;
        _read = read;
        _write = write;
    }

    public bool IsTrigger => Info.Type == OscParamType.Trigger;

    public object[] ReadValue()
    {
        _read?.Invoke(Info);
        return Info.EncodeValue();
    }

    /// <summary>Applies a value message from the phone. The result is not echoed back to the phone.</summary>
    public void Apply(OscMessage m)
    {
        if (!Info.ApplyValue(m)) return;

        Info.Sanitize();
        _write?.Invoke(Info);
        if (!IsTrigger) _lastSent = ReadValue();
    }

    /// <summary>True (with the new value) when the game-side value changed since it was last sent.</summary>
    public bool TryGetChange(out object[] value)
    {
        value = null;
        if (IsTrigger || _read == null) return false;

        object[] current = ReadValue();
        if (_lastSent != null && SameValue(_lastSent, current)) return false;

        _lastSent = current;
        value = current;
        return true;
    }

    private static bool SameValue(object[] a, object[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (!Equals(a[i], b[i])) return false;
        return true;
    }
}

/// <summary>
/// The set of parameters the Linux build exposes to the phone. <see cref="OscParameterSource"/>
/// components fill it in; the phone app renders whatever ends up here.
///
/// Ids without a leading '/' are placed under "/param/". Group/label are free text shown on the phone.
/// </summary>
public sealed class OscParameterRegistry
{
    private readonly List<OscParameter> _list = new List<OscParameter>();
    private readonly Dictionary<string, OscParameter> _byAddress = new Dictionary<string, OscParameter>();
    private readonly Dictionary<string, Action<OscMessage>> _raw = new Dictionary<string, Action<OscMessage>>();

    public IReadOnlyList<OscParameter> Parameters => _list;

    public void Clear()
    {
        _list.Clear();
        _byAddress.Clear();
        _raw.Clear();
    }

    public bool TryGet(string address, out OscParameter parameter) => _byAddress.TryGetValue(address, out parameter);
    public bool TryGetRaw(string address, out Action<OscMessage> handler) => _raw.TryGetValue(address, out handler);

    /// <summary>A handler for an arbitrary address that is not shown on the phone (e.g. an audio-reactive input).</summary>
    public void Raw(string address, Action<OscMessage> handler) => _raw[address] = handler;

    // ---------------------------------------------------------------- typed registration

    public OscParameter Float(string id, string label, float min, float max, Func<float> get, Action<float> set, string group = null)
    {
        var info = NewInfo(OscParamType.Float, id, label, group);
        info.Min = min;
        info.Max = max;
        return Add(info, i => i.Values[0] = get(), i => set(i.Values[0]));
    }

    public OscParameter Int(string id, string label, int min, int max, Func<int> get, Action<int> set, string group = null)
    {
        var info = NewInfo(OscParamType.Int, id, label, group);
        info.Min = min;
        info.Max = max;
        return Add(info, i => i.Values[0] = get(), i => set(Mathf.RoundToInt(i.Values[0])));
    }

    public OscParameter Bool(string id, string label, Func<bool> get, Action<bool> set, string group = null)
    {
        var info = NewInfo(OscParamType.Bool, id, label, group);
        return Add(info, i => i.Values[0] = get() ? 1f : 0f, i => set(i.Values[0] >= 0.5f));
    }

    /// <summary>Pick one of several options. The option list is read when the schema is sent.</summary>
    public OscParameter Choice(string id, string label, IList<string> options, Func<int> get, Action<int> set, string group = null)
    {
        var info = NewInfo(OscParamType.Enum, id, label, group);
        info.Options = new string[options.Count];
        options.CopyTo(info.Options, 0);
        return Add(info, i => i.Values[0] = get(), i => set(Mathf.RoundToInt(i.Values[0])));
    }

    public OscParameter Trigger(string id, string label, Action fire, string group = null)
    {
        var info = NewInfo(OscParamType.Trigger, id, label, group);
        return Add(info, null, _ => fire());
    }

    /// <summary>A min/max pair, shown as two sliders.</summary>
    public OscParameter Range(string id, string label, float min, float max, Func<Vector2> get, Action<Vector2> set, string group = null)
    {
        var info = NewInfo(OscParamType.Range, id, label, group);
        info.Min = min;
        info.Max = max;
        return Add(info,
            i => { Vector2 v = get(); i.Values[0] = v.x; i.Values[1] = v.y; },
            i => set(new Vector2(i.Values[0], i.Values[1])));
    }

    public OscParameter Color(string id, string label, Func<Color> get, Action<Color> set, string group = null)
    {
        var info = NewInfo(OscParamType.Color, id, label, group);
        return Add(info,
            i => { Color c = get(); i.Values[0] = c.r; i.Values[1] = c.g; i.Values[2] = c.b; },
            i => set(new Color(i.Values[0], i.Values[1], i.Values[2], 1f)));
    }

    /// <summary>Colour keys only (up to 8); alpha keys of the current gradient are kept when setting.</summary>
    public OscParameter Gradient(string id, string label, Func<Gradient> get, Action<Gradient> set, string group = null)
    {
        var info = NewInfo(OscParamType.Gradient, id, label, group);
        return Add(info,
            i => i.SetGradient(get()),
            i => set(i.ToGradient(get())));
    }

    // ---------------------------------------------------------------- internals

    private static OscParamInfo NewInfo(OscParamType type, string id, string label, string group) => new OscParamInfo(type)
    {
        Address = id.StartsWith("/") ? id : OscAddresses.ParamPrefix + id,
        Label = string.IsNullOrEmpty(label) ? id : label,
        Group = group ?? ""
    };

    private OscParameter Add(OscParamInfo info, Action<OscParamInfo> read, Action<OscParamInfo> write)
    {
        var parameter = new OscParameter(info, read, write);

        if (_byAddress.TryGetValue(info.Address, out OscParameter existing))
        {
            Debug.LogWarning($"[OSC] Parameter address {info.Address} registered twice; the later one wins");
            _list.Remove(existing);
        }

        _byAddress[info.Address] = parameter;
        _list.Add(parameter);
        return parameter;
    }
}

/// <summary>
/// Base class for components that decide which parameters the phone can control.
/// Put one (or several) in the Linux build's scene; <see cref="OSCServerController"/> finds them.
/// Enabling/disabling a source updates the phone's controls automatically.
/// </summary>
public abstract class OscParameterSource : MonoBehaviour
{
    public abstract void Register(OscParameterRegistry registry);

    /// <summary>Sources register in ascending order, which decides the order of the controls on the phone.</summary>
    public virtual int Order => 0;

    protected virtual void OnEnable() => OSCServerController.NotifySourcesChanged();
    protected virtual void OnDisable() => OSCServerController.NotifySourcesChanged();
}
