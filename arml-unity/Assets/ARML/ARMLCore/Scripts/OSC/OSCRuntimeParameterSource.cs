using System;
using System.Collections.Generic;
using System.Reflection;
using ARML.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Exposes every field marked with [RuntimeParameter] (the same attribute the in-game SettingsPanel
/// uses) to the OSC remote. No registration code is needed: add the attribute and it appears on the phone.
///
///   [RuntimeParameter(0f, 2f, "Speed")]  public float speed;
///
/// Supported field types: float, int, bool, enum, Vector2 (min/max pair), Color, Gradient.
/// Min/max from the attribute are the slider limits (floats, ints and Vector2). The attribute's
/// Scope is honoured: Global = one control that drives every instance of that type,
/// PerObject = one control per object. UseLogScale is ignored on the phone.
///
/// <see cref="OSCServerController"/> adds this component automatically if the scene has none.
/// </summary>
public class OSCRuntimeParameterSource : OscParameterSource
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    protected override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDisable();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => OSCServerController.NotifySourcesChanged();

    private sealed class Entry
    {
        public FieldInfo Field;
        public RuntimeParameterAttribute Attribute;
        public Type Owner;
        public readonly List<object> Targets = new List<object>();
    }

    public override void Register(OscParameterRegistry r)
    {
        var entries = new List<Entry>();
        var global = new Dictionary<string, Entry>();

        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID))
        {
            if (mb == null) continue;

            for (Type t = mb.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                foreach (FieldInfo field in t.GetFields(Flags))
                {
                    var attribute = field.GetCustomAttribute<RuntimeParameterAttribute>(true);
                    if (attribute == null || !IsSupported(field.FieldType)) continue;

                    if (attribute.Scope == ParameterScope.Global)
                    {
                        string key = $"{t.FullName}.{field.Name}";
                        if (!global.TryGetValue(key, out Entry entry))
                        {
                            entry = new Entry { Field = field, Attribute = attribute, Owner = t };
                            global[key] = entry;
                            entries.Add(entry);
                        }
                        entry.Targets.Add(mb);
                    }
                    else
                    {
                        var entry = new Entry { Field = field, Attribute = attribute, Owner = t };
                        entry.Targets.Add(mb);
                        entries.Add(entry);
                    }
                }
            }
        }

        foreach (Entry e in entries)
        {
            try { RegisterEntry(r, e); }
            catch (Exception ex) { Debug.LogWarning($"[OSC] {e.Owner.Name}.{e.Field.Name}: {ex.Message}"); }
        }
    }

    private static bool IsSupported(Type t) =>
        t == typeof(float) || t == typeof(int) || t == typeof(bool) || t.IsEnum ||
        t == typeof(Vector2) || t == typeof(Color) || t == typeof(Gradient);

    private static void RegisterEntry(OscParameterRegistry r, Entry e)
    {
        FieldInfo field = e.Field;
        RuntimeParameterAttribute a = e.Attribute;
        Type type = field.FieldType;
        List<object> targets = e.Targets;

        string name = string.IsNullOrEmpty(a.Label) ? field.Name : a.Label;
        string group = e.Owner.Name;
        string id;
        if (a.Scope == ParameterScope.Global)
        {
            id = $"rp/{e.Owner.FullName}.{field.Name}";
            if (targets.Count > 1) name += $" (x{targets.Count})";
        }
        else
        {
            var mb = (MonoBehaviour)targets[0];
            id = $"rp/{mb.GetInstanceID()}/{field.Name}";
            name = $"{mb.gameObject.name} / {name}";
        }

        // Read from the first target, write to all of them.
        object Get() => field.GetValue(targets[0]);
        void Set(object value)
        {
            foreach (object target in targets)
                if (target is UnityEngine.Object o && o != null) field.SetValue(target, value);
        }

        float min = a.Min, max = a.Max;

        if (type == typeof(float))
            r.Float(id, name, min, max, () => (float)Get(), v => Set(v), group);
        else if (type == typeof(int))
            r.Int(id, name, Mathf.RoundToInt(min), Mathf.RoundToInt(max), () => (int)Get(), v => Set(v), group);
        else if (type == typeof(bool))
            r.Bool(id, name, () => (bool)Get(), v => Set(v), group);
        else if (type.IsEnum)
        {
            string[] names = Enum.GetNames(type);
            Array values = Enum.GetValues(type);
            r.Choice(id, name, names, () => Array.IndexOf(values, Get()),
                i => Set(values.GetValue(Mathf.Clamp(i, 0, values.Length - 1))), group);
        }
        else if (type == typeof(Vector2))
            r.Range(id, name, min, max, () => (Vector2)Get(), v => Set(v), group);
        else if (type == typeof(Color))
            r.Color(id, name, () => (Color)Get(), v => Set(v), group);
        else if (type == typeof(Gradient))
            r.Gradient(id, name, () => (Gradient)Get(), v => Set(v), group);
    }
}
