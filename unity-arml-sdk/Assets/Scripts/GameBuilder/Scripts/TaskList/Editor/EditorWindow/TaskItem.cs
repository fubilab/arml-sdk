using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace ARML.GameBuilder
{
    public class TaskItem : VisualElement
    {
        Toggle taskToggle;
        Label taskLabel;

        public TaskItem(string taskText)
        {
            string filePath = FranUtils.GetAssetFilePathFromName(this.GetType().Name.ToString());

            VisualTreeAsset original = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(filePath.Substring(0, filePath.Length - 2) + "uxml");
            this.Add(original.Instantiate());

            taskToggle = this.Q<Toggle>();

            taskLabel = this.Q<Label>();
            taskLabel.text = taskText;
        }

        public Toggle GetTaskToggle()
        {
            return taskToggle;
        }

        public Label GetTaskLabel()
        {
            return taskLabel;
        }
    }
}