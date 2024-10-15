using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace ARML.DS.Utilities
{
    using Elements;
    using UnityEngine;

    public static class DSElementUtility
    {
        public static Button CreateButton(string text, Action onClick = null)
        {
            Button button = new Button(onClick)
            {
                text = text
            };

            return button;
        }

        public static Foldout CreateFoldout(string title, bool collapsed = false)
        {
            Foldout foldout = new Foldout()
            {
                text = title,
                value = !collapsed
            };

            return foldout;
        }

        public static Port CreatePort(this DSNode node, string portName = "", Orientation orientation = Orientation.Horizontal, Direction direction = Direction.Output, Port.Capacity capacity = Port.Capacity.Single)
        {
            Port port = node.InstantiatePort(orientation, direction, capacity, typeof(bool));

            port.portName = portName;

            return port;
        }

        public static TextField CreateTextField(string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textField = new TextField()
            {
                value = value,
                label = label
            };

            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }

        public static TextField CreateTextArea(string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textArea = CreateTextField(value, label, onValueChanged);

            textArea.multiline = true;

            return textArea;
        }

        public static ObjectField CreateObjectField(Object value = null, string label = null, Type objectType = null, EventCallback<ChangeEvent<Object>> onValueChanged = null)
        {
            ObjectField objectField = new ObjectField(label)
            {
                value = value
            };

            //If type was not specified, accept any
            if (objectType != null)
                objectField.objectType = objectType;

            if (onValueChanged != null)
            {
                objectField.RegisterValueChangedCallback(onValueChanged);
            }

            return objectField;
        }

        public static IntegerField CreateIntegerField(int value = 0, string label = null, int maxLength = 1, EventCallback<ChangeEvent<int>> onValueChanged = null)
        {
            IntegerField integerField = new IntegerField()
            {
                value = value,
                maxLength = maxLength,
                label = label
            };

            if (onValueChanged != null)
            {
                integerField.RegisterValueChangedCallback(onValueChanged);
            }

            return integerField;
        }

        //public static PropertyField CreatePropertyField
    }
}