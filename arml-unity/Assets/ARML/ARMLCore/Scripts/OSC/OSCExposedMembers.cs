using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Inspector-driven source: pick any component and a field/property/method name, and it becomes
/// a control on the phone. No code needed. Supported member types: float, int, bool, enum,
/// Vector2 (shown as a min/max pair), Color, Gradient, and parameterless methods (buttons).
///
/// Members are looked up by name through reflection, so if you build with IL2CPP stripping make
/// sure the members are kept (e.g. [UnityEngine.Scripting.Preserve]).
/// </summary>
public class OSCExposedMembers : OscParameterSource
{
    [Serializable]
    public class Entry
    {
        public Component target;
        [Tooltip("Field, property or method name on the component")]
        public string member;
        [Tooltip("Shown on the phone; defaults to the member name")]
        public string label;
        [Tooltip("Section header on the phone")]
        public string group;
        [Tooltip("Slider limits for float / int / Vector2 members")]
        public Vector2 range = new Vector2(0f, 1f);
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public override void Register(OscParameterRegistry r)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e.target == null || string.IsNullOrEmpty(e.member)) continue;

            try
            {
                if (!RegisterEntry(r, e, i))
                    Debug.LogWarning($"[OSC] {name}: '{e.member}' on {e.target.GetType().Name} not found or has an unsupported type");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OSC] {name}: '{e.member}': {ex.Message}");
            }
        }
    }

    private static bool RegisterEntry(OscParameterRegistry r, Entry e, int index)
    {
        Type owner = e.target.GetType();
        string id = $"member/{index}_{owner.Name}.{e.member}";
        string label = string.IsNullOrEmpty(e.label) ? e.member : e.label;
        string group = e.group;
        object target = e.target;

        MethodInfo method = owner.GetMethod(e.member, Flags, null, Type.EmptyTypes, null);
        if (method != null && method.ReturnType == typeof(void))
        {
            r.Trigger(id, label, () => method.Invoke(target, null), group);
            return true;
        }

        Func<object> get;
        Action<object> set;
        Type type;

        FieldInfo field = owner.GetField(e.member, Flags);
        PropertyInfo prop = field == null ? owner.GetProperty(e.member, Flags) : null;
        if (field != null)
        {
            type = field.FieldType;
            get = () => field.GetValue(target);
            set = v => field.SetValue(target, v);
        }
        else if (prop != null && prop.CanRead && prop.CanWrite && prop.GetIndexParameters().Length == 0)
        {
            type = prop.PropertyType;
            get = () => prop.GetValue(target);
            set = v => prop.SetValue(target, v);
        }
        else return false;

        float min = e.range.x, max = e.range.y;

        if (type == typeof(float))
            r.Float(id, label, min, max, () => (float)get(), v => set(v), group);
        else if (type == typeof(int))
            r.Int(id, label, Mathf.RoundToInt(min), Mathf.RoundToInt(max), () => (int)get(), v => set(v), group);
        else if (type == typeof(bool))
            r.Bool(id, label, () => (bool)get(), v => set(v), group);
        else if (type.IsEnum)
        {
            string[] names = Enum.GetNames(type);
            Array values = Enum.GetValues(type);
            r.Choice(id, label, names,
                () => Array.IndexOf(values, get()),
                i => set(values.GetValue(Mathf.Clamp(i, 0, values.Length - 1))), group);
        }
        else if (type == typeof(Vector2))
            r.Range(id, label, min, max, () => (Vector2)get(), v => set(v), group);
        else if (type == typeof(Color))
            r.Color(id, label, () => (Color)get(), v => set(v), group);
        else if (type == typeof(Gradient))
            r.Gradient(id, label, () => (Gradient)get(), v => set(v), group);
        else return false;

        return true;
    }
}
