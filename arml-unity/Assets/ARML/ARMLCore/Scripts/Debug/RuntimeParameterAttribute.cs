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
        public float Min { get; set; }
        public float Max { get; set; }
        public string Label { get; set; }
        public bool UseLogScale { get; set; }
        public ParameterScope Scope { get; set; }

        public RuntimeParameterAttribute(float min = 0f, float max = 1f, string label = null, bool useLogScale = false, ParameterScope scope = ParameterScope.Global)
        {
            Min = min;
            Max = max;
            Label = label;
            UseLogScale = useLogScale;
            Scope = scope;
        }
    }
}