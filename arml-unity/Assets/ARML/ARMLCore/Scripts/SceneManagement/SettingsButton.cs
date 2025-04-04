using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Reflection;
using UnityEngine.Serialization;

namespace ARML.SceneManagement
{
    [RequireComponent(typeof(Button))]
    public class SettingsButton : MonoBehaviour
    {
        public string fieldName; 
        public string targetFieldValue; // The value that should trigger the active color
        
        public Color activeColor = Color.cyan; // The color to set when condition is met
        public Color defaultColor = Color.white; // The color to set when condition is not met
        
        private Button button;
        private ApplicationLauncher launcher;
        private SettingsConfiguration targetObject;

        private void Start()
        {
            button = GetComponent<Button>();
            launcher = GetComponentInParent<ApplicationLauncher>();
            if (launcher == null)
            {
                Debug.LogWarning("No application launcher attached to parent");
                return;
            }
            button.onClick.AddListener(OnButtonClick);
            UpdateButtonColor();
        }

        private FieldInfo GetSettingsField(string fieldName)
        {
            if (launcher.settings == null)
            {
                Debug.LogWarning("Target Object is not set.");
                return null;
            }

            var field = launcher.settings.GetType().GetField(fieldName);
            if (field == null)
            {
                Debug.LogWarning($"Field '{fieldName}' not found.");
                return null;
            }
            return field;
        }

        private void UpdateButtonColor()
        {
            var field = GetSettingsField(fieldName);
            if (field == null) return;
            
            object fieldValue = field.GetValue(launcher.settings);

            if (fieldValue == null)
            {
                Debug.LogWarning($"Field '{fieldName}' is null.");
                return;
            }

            button.image.color = defaultColor;

            if (
                (fieldValue is string strValue && strValue == targetFieldValue) ||
                (fieldValue is bool boolValue && string.Equals(boolValue.ToString(), targetFieldValue, StringComparison.CurrentCultureIgnoreCase)) ||
                (fieldValue is int intValue && intValue.ToString() == targetFieldValue) ||
                (fieldValue is float floatValue && floatValue.ToString() == targetFieldValue) ||
                (fieldValue.GetType().IsEnum && fieldValue.ToString() == targetFieldValue)
            )
            {
                button.image.color = activeColor;
            }
        }

        private void UpdateSettingsObject()
        {
            var field = GetSettingsField(fieldName);
            if (field == null) return;
            var fieldType = field.FieldType;

            if (fieldType == typeof(string))
            {
                field.SetValue(launcher.settings, targetFieldValue);
            }
            else if (fieldType == typeof(bool))
            {
                field.SetValue(launcher.settings, targetFieldValue.ToLower() == "true");
            }
            else if (fieldType == typeof(int))
            {
                field.SetValue(launcher.settings, int.Parse(targetFieldValue));
            }
            else if (fieldType == typeof(float))
            {
                field.SetValue(launcher.settings, float.Parse(targetFieldValue));
            }
            else if (fieldType.IsEnum)
            {
                field.SetValue(launcher.settings, Enum.Parse(fieldType, targetFieldValue));
            }
            else
            {
                Debug.LogWarning($"Field '{fieldName}' is not a recognized type.");
            }
        }

        private void OnButtonClick()
        {
            UpdateSettingsObject();
            launcher.SaveSettings();
            foreach (var settingsButton in launcher.gameObject.GetComponentsInChildren<SettingsButton>())
            {
                settingsButton.UpdateButtonColor();
            }
        }
    }
}