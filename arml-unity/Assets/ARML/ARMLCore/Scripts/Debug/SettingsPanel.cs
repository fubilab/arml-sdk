using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARML.Attributes;
using ARML.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace ARML.DebugTools
{
    /// <summary>
    /// Automatically generates runtime UI sliders for fields marked with [RuntimeTweakable] attribute.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject tweakerPanel;
        [SerializeField] private Transform slidersContainer;
        [FormerlySerializedAs("sliderPrefab")] [SerializeField] private GameObject sliderTemplate;

        private List<TweakableField> tweakableFields = new List<TweakableField>();
        private EventSystem eventSystem;
        private Slider firstSlider; // Store reference to first slider
        private bool isPanelVisible = false;
        private List<GameObject> spawnedSliders = new List<GameObject>();

        private class TweakableField
        {
            public List<object> targetObjects = new List<object>(); // Multiple objects for global scope
            public FieldInfo fieldInfo;
            public RuntimeTweakableAttribute attribute;
            public Slider slider;
            public TMP_Text valueText;
            public TMP_Text labelText;
        }
        
        private void Awake()
        {
            // Get or find EventSystem
            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = FindObjectOfType<EventSystem>();
            }
            
            // Find all objects with RuntimeTweakable fields
            DiscoverTweakableFields();
            GenerateUI();
        }
        
        private void Start()
        {
            SceneController.Instance.OnGameSceneLoaded += (scene) => ReScanTweakables();
        }

        private void ReScanTweakables()
        {
            DiscoverTweakableFields();
            GenerateUI();
        }

        /// <summary>
        /// Discovers all fields marked with [RuntimeTweakable] in the scene.
        /// </summary>
        private void DiscoverTweakableFields()
        {
            tweakableFields.Clear();

            // Find all MonoBehaviours in the scene
            MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            
            Debug.Log($"RuntimeTweaker: Scanning {allMonoBehaviours.Length} MonoBehaviours...");

            // Group by type and field for global scope handling
            Dictionary<string, TweakableField> globalFields = new Dictionary<string, TweakableField>();

            int totalFieldsScanned = 0;
            int attributedFieldsFound = 0;

            foreach (MonoBehaviour mb in allMonoBehaviours)
            {
                if (mb == null) continue;

                Type type = mb.GetType();
                
                // Try both binding flags combinations for better compatibility
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var allFields = new List<FieldInfo>();
                Type t = type;
                while (t != null && t != typeof(MonoBehaviour) && t != typeof(UnityEngine.Object))
                {
                    allFields.AddRange(t.GetFields(flags | BindingFlags.DeclaredOnly));
                    t = t.BaseType;
                }
                FieldInfo[] fields = allFields.ToArray();
                
                totalFieldsScanned += fields.Length;

                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        RuntimeTweakableAttribute attribute = null;
                        
                        // Extract raw applied attributes to avoid strict runtime assembly translation exceptions
                        object[] customAttributes = field.GetCustomAttributes(true);
                        foreach (object attr in customAttributes)
                        {
                            if (attr is RuntimeTweakableAttribute || attr.GetType().FullName == "ARML.Attributes.RuntimeTweakableAttribute")
                            {
                                attribute = attr as RuntimeTweakableAttribute;
                                break;
                            }
                        }
                        
                        if (attribute != null && field.FieldType == typeof(float))
                        {
                            attributedFieldsFound++;
                            Debug.Log($"RuntimeTweaker: Found tweakable field: {type.Name}.{field.Name}");
                            
                            if (attribute.Scope == TweakableScope.Global)
                            {
                                // Create a unique key for this field type
                                string key = $"{type.FullName}.{field.Name}";

                                if (!globalFields.ContainsKey(key))
                                {
                                    globalFields[key] = new TweakableField
                                    {
                                        fieldInfo = field,
                                        attribute = attribute
                                    };
                                }

                                // Add this object to the list of targets
                                globalFields[key].targetObjects.Add(mb);
                            }
                            else // PerObject
                            {
                                tweakableFields.Add(new TweakableField
                                {
                                    targetObjects = new List<object> { mb },
                                    fieldInfo = field,
                                    attribute = attribute
                                });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"RuntimeTweaker: Error processing field {type.Name}.{field.Name}: {e.Message}");
                    }
                }
            }

            // Add all global fields to the main list
            tweakableFields.AddRange(globalFields.Values);

            Debug.Log($"RuntimeTweaker: Scanned {totalFieldsScanned} fields in {allMonoBehaviours.Length} MonoBehaviours");
            Debug.Log($"RuntimeTweaker: Found {attributedFieldsFound} fields with [RuntimeTweakable]");
            Debug.Log($"RuntimeTweaker: Created {tweakableFields.Count} sliders ({globalFields.Count} global, {tweakableFields.Count - globalFields.Count} per-object)");
        }

        private void GenerateUI()
        {
            if (sliderTemplate == null || slidersContainer == null)
            {
                Debug.LogWarning("RuntimeTweaker: Slider prefab or container not assigned!");
                return;
            }
            
            //First Remove previous spawned sliders if any
            foreach (GameObject slider in spawnedSliders)
            {
                Destroy(slider);
            }
            spawnedSliders.Clear();

            bool isFirst = true;
            
            sliderTemplate.SetActive(true);

            foreach (TweakableField tweakable in tweakableFields)
            {
                GameObject sliderInstance = Instantiate(sliderTemplate, slidersContainer);
                
                // Get components from the prefab
                Slider slider = sliderInstance.GetComponentInChildren<Slider>();
                TMP_Text labelText = sliderInstance.transform.Find("Label")?.GetComponent<TMP_Text>();
                TMP_Text valueText = sliderInstance.transform.Find("Value")?.GetComponent<TMP_Text>();
                
                spawnedSliders.Add(sliderInstance);

                if (slider == null)
                {
                    Debug.LogError("RuntimeTweaker: Slider component not found in prefab!");
                    continue;
                }

                // Store reference to first slider for EventSystem selection
                if (isFirst)
                {
                    firstSlider = slider;
                    isFirst = false;
                }

                // Setup slider
                slider.minValue = tweakable.attribute.MinValue;
                slider.maxValue = tweakable.attribute.MaxValue;
                
                // Get current value from first object
                float currentValue = (float)tweakable.fieldInfo.GetValue(tweakable.targetObjects[0]);
                
                if (tweakable.attribute.UseLogarithmicScale)
                {
                    slider.value = LinearToLogarithmic(currentValue, slider.minValue, slider.maxValue);
                }
                else
                {
                    slider.value = currentValue;
                }

                // Setup label
                if (labelText != null)
                {
                    tweakable.labelText = labelText;
                    UpdateLabel(tweakable);
                }

                // Setup value text
                if (valueText != null)
                {
                    tweakable.valueText = valueText;
                    UpdateValueText(tweakable, currentValue);
                }

                // Store references
                tweakable.slider = slider;

                // Add listener
                slider.onValueChanged.AddListener((value) => OnSliderValueChanged(tweakable, value));
            }
            
            sliderTemplate.SetActive(false);
        }

        /// <summary>
        /// Updates the label text based on scope.
        /// </summary>
        private void UpdateLabel(TweakableField tweakable)
        {
            if (tweakable.labelText == null) return;

            string displayName = tweakable.attribute.DisplayName ?? tweakable.fieldInfo.Name;
            
            if (tweakable.attribute.Scope == TweakableScope.Global)
            {
                Type type = tweakable.targetObjects[0].GetType();
                tweakable.labelText.text = $"[GLOBAL] {type.Name}.{displayName} ({tweakable.targetObjects.Count})";
            }
            else
            {
                MonoBehaviour mb = (MonoBehaviour)tweakable.targetObjects[0];
                tweakable.labelText.text = $"{mb.gameObject.name}.{displayName}";
            }
        }
        
        /// <summary>
        /// Toggles the tweaker panel visibility.
        /// </summary>
        public void SetPanelVisibility(bool state)
        {
            isPanelVisible = state;
            if (tweakerPanel != null)
            {
                tweakerPanel.SetActive(isPanelVisible);

                // Select first slider when panel opens
                if (isPanelVisible && firstSlider != null && eventSystem != null)
                {
                    eventSystem.SetSelectedGameObject(firstSlider.gameObject);
                }
                else if (!isPanelVisible && eventSystem != null)
                {
                    // Clear selection when closing
                    eventSystem.SetSelectedGameObject(null);
                }
            }
        }

        /// <summary>
        /// Called when a slider value changes.
        /// </summary>
        private void OnSliderValueChanged(TweakableField tweakable, float sliderValue)
        {
            float actualValue = sliderValue;
            
            if (tweakable.attribute.UseLogarithmicScale)
            {
                actualValue = LogarithmicToLinear(sliderValue, tweakable.attribute.MinValue, tweakable.attribute.MaxValue);
            }

            // Set the value on ALL target objects (for global, this updates all instances)
            foreach (object targetObject in tweakable.targetObjects)
            {
                tweakable.fieldInfo.SetValue(targetObject, actualValue);
            }
            
            // Update value text
            UpdateValueText(tweakable, actualValue);
        }

        /// <summary>
        /// Updates the value display text.
        /// </summary>
        private void UpdateValueText(TweakableField tweakable, float value)
        {
            if (tweakable.valueText != null)
            {
                // Format based on magnitude
                if (Mathf.Abs(value) < 0.001f)
                    tweakable.valueText.text = value.ToString("F6");
                else if (Mathf.Abs(value) < 0.1f)
                    tweakable.valueText.text = value.ToString("F4");
                else if (Mathf.Abs(value) < 10f)
                    tweakable.valueText.text = value.ToString("F2");
                else
                    tweakable.valueText.text = value.ToString("F0");
            }
        }
        
        /// <summary>
        /// Converts a linear value to logarithmic scale for the slider.
        /// </summary>
        private float LinearToLogarithmic(float value, float min, float max)
        {
            float logMin = Mathf.Log10(min);
            float logMax = Mathf.Log10(max);
            float logValue = Mathf.Log10(value);
            return Mathf.InverseLerp(logMin, logMax, logValue) * (max - min) + min;
        }

        /// <summary>
        /// Converts a slider value from logarithmic scale back to linear.
        /// </summary>
        private float LogarithmicToLinear(float sliderValue, float min, float max)
        {
            float t = Mathf.InverseLerp(min, max, sliderValue);
            float logMin = Mathf.Log10(min);
            float logMax = Mathf.Log10(max);
            float logValue = Mathf.Lerp(logMin, logMax, t);
            return Mathf.Pow(10, logValue);
        }

        /// <summary>
        /// Refreshes all discovered fields and regenerates UI.
        /// </summary>
        [ContextMenu("Refresh Tweakables")]
        public void RefreshTweakables()
        {
            // Clear old UI
            foreach (Transform child in slidersContainer)
            {
                Destroy(child.gameObject);
            }

            // Rediscover and regenerate
            DiscoverTweakableFields();
            GenerateUI();
        }
    }
}