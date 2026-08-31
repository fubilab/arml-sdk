using System;
using UnityEngine;

namespace ARML.Attributes
{
    /// <summary>
    /// Scope for runtime tweakable fields.
    /// </summary>
    public enum ParameterScope
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
    public class RuntimeParameterAttribute : Attribute
    {
        public float MinValue { get; private set; }
        public float MaxValue { get; private set; }
        public string DisplayName { get; private set; }
        public bool UseLogarithmicScale { get; private set; }
        public ParameterScope Scope { get; private set; }

        public RuntimeParameterAttribute(float minValue = 0f, float maxValue = 1f, string displayName = null, bool useLogarithmicScale = false, ParameterScope scope = ParameterScope.Global)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            DisplayName = displayName;
            UseLogarithmicScale = useLogarithmicScale;
            Scope = scope;
        }
    }
}