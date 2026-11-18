using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum OscParamType { Float, Int, Bool, Enum, Trigger, Range, Color, Gradient }

/// <summary>
/// Description and current value of one remotely controllable parameter. Shared by both ends:
/// the server builds these from its registered parameters and sends them as the schema, the
/// phone rebuilds them from the schema and renders a control for each.
///
/// Value layout per type (also the OSC arguments of value messages):
///   Float     f value                      Int      i value
///   Bool      i 0/1                        Enum     i index
///   Range     f min, f max                 Color    f r, f g, f b
///   Trigger   (i 1 when fired)             Gradient i mode, then (f r, f g, f b, f time) * n
///
/// Schema message args: i revision, s address, s label, s type, s group, then
///   Float/Range/Int: min, max, value...   Bool/Color/Gradient: value...   Enum: value, options...
/// </summary>
public sealed class OscParamInfo
{
    public const int MaxGradientKeys = 8;
    private const int SchemaHeader = 5; // revision, address, label, type, group

    public string Address = "";
    public string Label = "";
    public string Group = "";
    public OscParamType Type;

    // Float / Int / Range bounds.
    public float Min = 0f;
    public float Max = 1f;

    public string[] Options = Array.Empty<string>();      // Enum
    public float[] Values = Array.Empty<float>();          // everything except Gradient / Trigger
    public int GradientMode;                               // 0 blend, 1 fixed, 2 perceptual
    public readonly List<Vector4> GradientKeys = new List<Vector4>(); // r, g, b, time

    public OscParamInfo(OscParamType type)
    {
        Type = type;
        Values = new float[ValueCount(type)];
    }

    public static int ValueCount(OscParamType type)
    {
        switch (type)
        {
            case OscParamType.Float:
            case OscParamType.Int:
            case OscParamType.Bool:
            case OscParamType.Enum: return 1;
            case OscParamType.Range: return 2;
            case OscParamType.Color: return 3;
            default: return 0;
        }
    }

    private static bool UsesBounds(OscParamType t) =>
        t == OscParamType.Float || t == OscParamType.Int || t == OscParamType.Range;

    private static bool IsInteger(OscParamType t) =>
        t == OscParamType.Int || t == OscParamType.Bool || t == OscParamType.Enum;

    // ---------------------------------------------------------------- encoding

    public object[] EncodeValue()
    {
        switch (Type)
        {
            case OscParamType.Trigger:
                return new object[] { 1 };

            case OscParamType.Gradient:
            {
                var args = new object[1 + GradientKeys.Count * 4];
                args[0] = GradientMode;
                for (int i = 0; i < GradientKeys.Count; i++)
                {
                    Vector4 k = GradientKeys[i];
                    args[1 + i * 4] = k.x;
                    args[2 + i * 4] = k.y;
                    args[3 + i * 4] = k.z;
                    args[4 + i * 4] = k.w;
                }
                return args;
            }

            default:
            {
                var args = new object[Values.Length];
                for (int i = 0; i < args.Length; i++)
                    args[i] = IsInteger(Type) ? (object)Mathf.RoundToInt(Values[i]) : Values[i];
                return args;
            }
        }
    }

    public object[] EncodeSchema(int revision)
    {
        var args = new List<object> { revision, Address, Label, TypeName(Type), Group ?? "" };

        if (UsesBounds(Type))
        {
            if (Type == OscParamType.Int)
            {
                args.Add(Mathf.RoundToInt(Min));
                args.Add(Mathf.RoundToInt(Max));
            }
            else
            {
                args.Add(Min);
                args.Add(Max);
            }
        }

        args.AddRange(EncodeValue());
        if (Type == OscParamType.Enum)
            foreach (string option in Options) args.Add(option);

        return args.ToArray();
    }

    // ---------------------------------------------------------------- decoding

    /// <summary>Reads value arguments starting at <paramref name="offset"/>. False if they are incomplete.</summary>
    public bool ApplyValue(OscMessage m, int offset = 0)
    {
        switch (Type)
        {
            case OscParamType.Trigger:
                return true;

            case OscParamType.Gradient:
            {
                if (!m.TryGetInt(offset, out int mode)) return false;
                GradientMode = mode;
                GradientKeys.Clear();
                for (int o = offset + 1; o + 3 < m.ArgCount; o += 4)
                {
                    m.TryGetFloat(o, out float r);
                    m.TryGetFloat(o + 1, out float g);
                    m.TryGetFloat(o + 2, out float b);
                    m.TryGetFloat(o + 3, out float t);
                    GradientKeys.Add(new Vector4(r, g, b, t));
                }
                return true;
            }

            default:
            {
                if (Values.Length == 0 || Values.Length != ValueCount(Type)) Values = new float[ValueCount(Type)];
                var parsed = new float[Values.Length];
                for (int i = 0; i < parsed.Length; i++)
                    if (!m.TryGetFloat(offset + i, out parsed[i])) return false;
                Values = parsed;
                return true;
            }
        }
    }

    /// <summary>Builds a parameter from a schema message; null if it is malformed.</summary>
    public static OscParamInfo FromSchema(OscMessage m)
    {
        if (!m.TryGetString(1, out string address) || !m.TryGetString(2, out string label) ||
            !m.TryGetString(3, out string typeName) || !m.TryGetString(4, out string group) ||
            !TryParseType(typeName, out OscParamType type))
            return null;

        var info = new OscParamInfo(type) { Address = address, Label = label, Group = group };
        int o = SchemaHeader;

        if (UsesBounds(type))
        {
            if (!m.TryGetFloat(o, out info.Min) || !m.TryGetFloat(o + 1, out info.Max)) return null;
            o += 2;
        }

        if (!info.ApplyValue(m, o)) return null;

        if (type == OscParamType.Enum)
        {
            var options = new List<string>();
            for (int i = o + 1; i < m.ArgCount; i++)
                if (m.TryGetString(i, out string option)) options.Add(option);
            info.Options = options.ToArray();
        }

        return info;
    }

    public void CopyValueFrom(OscParamInfo other)
    {
        Values = (float[])other.Values.Clone();
        GradientMode = other.GradientMode;
        GradientKeys.Clear();
        GradientKeys.AddRange(other.GradientKeys);
        Options = other.Options;
    }

    // ---------------------------------------------------------------- server-side helpers

    /// <summary>Forces the value into the declared limits (values come from the network).</summary>
    public void Sanitize()
    {
        switch (Type)
        {
            case OscParamType.Float:
            case OscParamType.Range:
                for (int i = 0; i < Values.Length; i++) Values[i] = Mathf.Clamp(Values[i], Min, Max);
                break;
            case OscParamType.Int:
                Values[0] = Mathf.Clamp(Mathf.Round(Values[0]), Mathf.Round(Min), Mathf.Round(Max));
                break;
            case OscParamType.Bool:
                Values[0] = Values[0] >= 0.5f ? 1f : 0f;
                break;
            case OscParamType.Enum:
                Values[0] = Mathf.Clamp(Mathf.Round(Values[0]), 0, Mathf.Max(0, Options.Length - 1));
                break;
            case OscParamType.Color:
                for (int i = 0; i < Values.Length; i++) Values[i] = Mathf.Clamp01(Values[i]);
                break;
            case OscParamType.Gradient:
                GradientMode = Mathf.Clamp(GradientMode, 0, 2);
                if (GradientKeys.Count > MaxGradientKeys)
                    GradientKeys.RemoveRange(MaxGradientKeys, GradientKeys.Count - MaxGradientKeys);
                if (GradientKeys.Count == 0) GradientKeys.Add(new Vector4(1, 1, 1, 0));
                for (int i = 0; i < GradientKeys.Count; i++)
                {
                    Vector4 k = GradientKeys[i];
                    GradientKeys[i] = new Vector4(Mathf.Clamp01(k.x), Mathf.Clamp01(k.y), Mathf.Clamp01(k.z), Mathf.Clamp01(k.w));
                }
                break;
        }
    }

    public void SetGradient(Gradient g)
    {
        GradientKeys.Clear();
        if (g == null) return;

        GradientMode = (int)g.mode;
        foreach (GradientColorKey k in g.colorKeys)
            GradientKeys.Add(new Vector4(k.color.r, k.color.g, k.color.b, k.time));
    }

    /// <summary>Builds a Gradient from the keys; alpha keys are taken from <paramref name="alphaFrom"/> if given.</summary>
    public Gradient ToGradient(Gradient alphaFrom = null)
    {
        var keys = new GradientColorKey[Mathf.Max(1, GradientKeys.Count)];
        if (GradientKeys.Count == 0) keys[0] = new GradientColorKey(Color.white, 0f);
        for (int i = 0; i < GradientKeys.Count; i++)
        {
            Vector4 k = GradientKeys[i];
            keys[i] = new GradientColorKey(new Color(k.x, k.y, k.z, 1f), Mathf.Clamp01(k.w));
        }

        return new Gradient
        {
            mode = (GradientMode)Mathf.Clamp(GradientMode, 0, 2),
            colorKeys = keys,
            alphaKeys = alphaFrom != null && alphaFrom.alphaKeys.Length > 0
                ? alphaFrom.alphaKeys
                : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        };
    }

    // ---------------------------------------------------------------- type names

    public static string TypeName(OscParamType t) => t.ToString().ToLowerInvariant();

    public static bool TryParseType(string name, out OscParamType type) =>
        Enum.TryParse(name, true, out type);

    public static string FormatFor(float min, float max)
    {
        float span = Mathf.Abs(max - min);
        if (span >= 100f) return "0";
        if (span >= 10f) return "0.0";
        if (span >= 1f) return "0.00";
        return "0.000";
    }

    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0} ({1})", Address, Type);
}
