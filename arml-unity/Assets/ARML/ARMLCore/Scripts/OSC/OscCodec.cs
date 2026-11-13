using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;

/// <summary>
/// Fixed addresses of the generic remote-control protocol. Everything else (the actual
/// parameters) is described by the server at runtime through the schema messages, so the
/// phone app never needs to know about specific parameters.
/// </summary>
public static class OscAddresses
{
    public const int ProtocolVersion = 1;

    // Phone -> server
    public const string Hello = "/osc/hello";   // subscribe + request the schema
    public const string Ping = "/osc/ping";     // keep-alive, answered with Pong
    public const string Bye = "/osc/bye";       // unsubscribe (sent when the phone switches to another server)

    // Server -> phone
    public const string Pong = "/osc/pong";
    public const string SchemaBegin = "/osc/schema/begin"; // i protocol, i parameterCount, i revision
    public const string SchemaParam = "/osc/schema/param"; // i revision, s address, s label, s type, s group, type specific...

    // Parameter value messages (both directions) are sent to the parameter's own address.
    // Default address prefix for parameters registered with a relative id.
    public const string ParamPrefix = "/param/";
}

public readonly struct OscMessage
{
    public readonly string Address;
    public readonly object[] Args;
    public readonly IPEndPoint Remote;

    public OscMessage(string address, object[] args, IPEndPoint remote = null)
    {
        Address = address;
        Args = args;
        Remote = remote;
    }

    public int ArgCount => Args?.Length ?? 0;

    public bool TryGetFloat(int index, out float value)
    {
        value = 0f;
        if (Args == null || index < 0 || index >= Args.Length) return false;

        switch (Args[index])
        {
            case float f: value = f; return true;
            case int i: value = i; return true;
            case double d: value = (float)d; return true;
            case long l: value = l; return true;
            case bool b: value = b ? 1f : 0f; return true;
            default: return false;
        }
    }

    public bool TryGetInt(int index, out int value)
    {
        value = 0;
        if (!TryGetFloat(index, out float f)) return false;
        value = (int)Math.Round(f);
        return true;
    }

    public bool TryGetBool(int index, out bool value)
    {
        value = false;
        if (!TryGetFloat(index, out float f)) return false;
        value = f >= 0.5f;
        return true;
    }

    public bool TryGetString(int index, out string value)
    {
        value = null;
        if (Args == null || index < 0 || index >= Args.Length) return false;
        value = Args[index] as string;
        return value != null;
    }
}

/// <summary>Minimal OSC 1.0 parser (messages and nested bundles) and encoder.</summary>
public static class OscCodec
{
    // ---------------------------------------------------------------- parsing

    public static void Parse(byte[] data, int length, IPEndPoint remote, ConcurrentQueue<OscMessage> output)
    {
        if (length < 4) return;

        if (StartsWithBundle(data, length))
        {
            int pos = 16; // "#bundle\0" + 8-byte timetag
            while (pos + 4 <= length)
            {
                int size = ReadInt(data, ref pos);
                if (size <= 0 || pos + size > length) return;

                var element = new byte[size];
                Buffer.BlockCopy(data, pos, element, 0, size);
                Parse(element, size, remote, output);
                pos += size;
            }
            return;
        }

        int p = 0;
        string address = ReadString(data, length, ref p);
        if (address.Length == 0 || address[0] != '/') return;

        // Messages without a type tag string are legal (old OSC); treat as no args.
        var args = new List<object>();
        if (p < length && data[p] == (byte)',')
        {
            string tags = ReadString(data, length, ref p);
            for (int i = 1; i < tags.Length; i++)
            {
                switch (tags[i])
                {
                    case 'f': args.Add(ReadFloat(data, length, ref p)); break;
                    case 'i': args.Add(ReadInt(data, length, ref p)); break;
                    case 'd': args.Add(ReadDouble(data, length, ref p)); break;
                    case 'h': args.Add(ReadLong(data, length, ref p)); break;
                    case 's': args.Add(ReadString(data, length, ref p)); break;
                    case 'T': args.Add(true); break;
                    case 'F': args.Add(false); break;
                    case 'N': args.Add(null); break;
                    case 'b':
                        int n = ReadInt(data, length, ref p);
                        if (n < 0 || p + n > length) throw new FormatException("blob size");
                        p += (n + 3) & ~3;
                        args.Add(null);
                        break;
                    default: throw new FormatException($"unsupported type tag '{tags[i]}'");
                }
            }
        }

        output.Enqueue(new OscMessage(address, args.ToArray(), remote));
    }

    private static bool StartsWithBundle(byte[] d, int length)
    {
        if (length < 16) return false;
        const string tag = "#bundle";
        for (int i = 0; i < tag.Length; i++)
            if (d[i] != tag[i]) return false;
        return d[7] == 0;
    }

    private static string ReadString(byte[] d, int length, ref int pos)
    {
        int end = pos;
        while (end < length && d[end] != 0) end++;
        if (end >= length) throw new FormatException("unterminated string");

        string s = Encoding.UTF8.GetString(d, pos, end - pos);
        pos += ((end - pos) + 4) & ~3; // string + NUL, padded to 4 bytes
        return s;
    }

    private static int ReadInt(byte[] d, ref int pos)
    {
        int v = (d[pos] << 24) | (d[pos + 1] << 16) | (d[pos + 2] << 8) | d[pos + 3];
        pos += 4;
        return v;
    }

    private static int ReadInt(byte[] d, int length, ref int pos)
    {
        Require(length, pos, 4);
        return ReadInt(d, ref pos);
    }

    private static long ReadLong(byte[] d, int length, ref int pos)
    {
        Require(length, pos, 8);
        long hi = (uint)ReadInt(d, ref pos);
        long lo = (uint)ReadInt(d, ref pos);
        return (hi << 32) | lo;
    }

    private static float ReadFloat(byte[] d, int length, ref int pos)
    {
        return BitConverter.Int32BitsToSingle(ReadInt(d, length, ref pos));
    }

    private static double ReadDouble(byte[] d, int length, ref int pos)
    {
        return BitConverter.Int64BitsToDouble(ReadLong(d, length, ref pos));
    }

    private static void Require(int length, int pos, int count)
    {
        if (pos + count > length) throw new FormatException("truncated packet");
    }

    // ---------------------------------------------------------------- encoding

    /// <summary>Encodes one message. Supported args: float, int, bool, string.</summary>
    public static byte[] EncodeMessage(string address, params object[] args)
    {
        var buffer = new List<byte>(64);
        WriteString(buffer, address);

        var tags = new StringBuilder(",");
        foreach (object a in args)
        {
            switch (a)
            {
                case float _: tags.Append('f'); break;
                case int _: tags.Append('i'); break;
                case string _: tags.Append('s'); break;
                case bool b: tags.Append(b ? 'T' : 'F'); break;
                default: throw new ArgumentException($"unsupported OSC argument type {a?.GetType()}");
            }
        }
        WriteString(buffer, tags.ToString());

        foreach (object a in args)
        {
            switch (a)
            {
                case float f: WriteInt(buffer, BitConverter.SingleToInt32Bits(f)); break;
                case int i: WriteInt(buffer, i); break;
                case string s: WriteString(buffer, s); break;
            }
        }

        return buffer.ToArray();
    }

    /// <summary>Wraps already-encoded messages in a single bundle (one UDP datagram).</summary>
    public static byte[] EncodeBundle(IList<byte[]> messages)
    {
        var buffer = new List<byte>(256);
        WriteString(buffer, "#bundle");
        for (int i = 0; i < 7; i++) buffer.Add(0); // timetag 1 = "immediately"
        buffer.Add(1);

        foreach (byte[] m in messages)
        {
            WriteInt(buffer, m.Length);
            buffer.AddRange(m);
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Packs messages into as few bundles as possible, none larger than maxBytes (unless a single
    /// message is bigger), so each one fits in a UDP datagram without fragmentation.
    /// </summary>
    public static List<byte[]> EncodeBundles(IList<byte[]> messages, int maxBytes = 1200)
    {
        var result = new List<byte[]>();
        var current = new List<byte[]>();
        int size = 16;

        foreach (byte[] m in messages)
        {
            if (current.Count > 0 && size + 4 + m.Length > maxBytes)
            {
                result.Add(EncodeBundle(current));
                current.Clear();
                size = 16;
            }
            current.Add(m);
            size += 4 + m.Length;
        }

        if (current.Count > 0) result.Add(EncodeBundle(current));
        return result;
    }

    private static void WriteInt(List<byte> b, int v)
    {
        b.Add((byte)(v >> 24));
        b.Add((byte)(v >> 16));
        b.Add((byte)(v >> 8));
        b.Add((byte)v);
    }

    private static void WriteString(List<byte> b, string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        b.AddRange(bytes);
        int pad = 4 - (bytes.Length % 4); // always at least one NUL
        for (int i = 0; i < pad; i++) b.Add(0);
    }
}
