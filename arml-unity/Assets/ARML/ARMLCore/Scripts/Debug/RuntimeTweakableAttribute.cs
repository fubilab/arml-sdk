using System;
using UnityEngine;

namespace ARML.Attributes
{
    /// <summary>
    /// Scope for runtime tweakable fields.
    /// </summary>
    public enum TweakableScope
    {
        /// <summary>One slider per object instance</summary>
        PerObject,
        /// <summary>One slider that affects all instances of the same type</summary>
        Global
    }
    
    /// <summary>
    /// Marks a field as tweakable at runtime via UI sliders.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RuntimeTweakableAttribute : Attribute
    {
        public float MinValue { get; private set; }
        public float MaxValue { get; private set; }
        public string DisplayName { get; private set; }
        public bool UseLogarithmicScale { get; private set; }
        public TweakableScope Scope { get; private set; }

        public RuntimeTweakableAttribute(float minValue = 0f, float maxValue = 1f, string displayName = null, bool useLogarithmicScale = false, TweakableScope scope = TweakableScope.Global)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            DisplayName = displayName;
            UseLogarithmicScale = useLogarithmicScale;
            Scope = scope;
        }
    }
}