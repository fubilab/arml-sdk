using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ARML.DS.Elements
{
    using Data.Save;
    using Enumerations;
    using Utilities;
    using Windows;

    public class DSNode : Node
    {
        public string ID { get; set; }
        public string DialogueName { get; set; }
        public List<DSChoiceSaveData> Choices { get; set; }
        public string TextEN { get; set; }
        public string TextES { get; set; }
        public string TextCA { get; set; }
        public DSDialogueType DialogueType { get; set; }
        public DSGroup Group { get; set; }
        public AudioClip AudioClipEN { get; set; }
        public AudioClip AudioClipES { get; set; }
        public AudioClip AudioClipCA { get; set; }
        public AnimationClip AnimationClip { get; set; }
        public int EventID { get; set; }

        protected DSGraphView graphView;
        private Color defaultBackgroundColor;

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Disconnect Input Ports", actionEvent => DisconnectInputPorts());
            evt.menu.AppendAction("Disconnect Output Ports", actionEvent => DisconnectOutputPorts());

            base.BuildContextualMenu(evt);
        }

        public virtual void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 position)
        {
            ID = Guid.NewGuid().ToString();

            DialogueName = nodeName;
            Choices = new List<DSChoiceSaveData>();
            TextEN = "Dialogue text.";
            TextES = "Dialogue text.";
            TextCA = "Dialogue text.";

            SetPosition(new Rect(position, Vector2.zero));

            graphView = dsGraphView;
            defaultBackgroundColor = new Color(29f / 255f, 29f / 255f, 30f / 255f);

            mainContainer.AddToClassList("ds-node__main-container");
            extensionContainer.AddToClassList("ds-node__extension-container");
        }

        public virtual void Draw()
        {
            /* TITLE CONTAINER */

            TextField dialogueNameTextField = DSElementUtility.CreateTextField(DialogueName, null, callback =>
            {
                TextField target = (TextField)callback.target;

                target.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();

                if (string.IsNullOrEmpty(target.value))
                {
                    if (!string.IsNullOrEmpty(DialogueName))
                    {
                        ++graphView.NameErrorsAmount;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(DialogueName))
                    {
                        --graphView.NameErrorsAmount;
                    }
                }

                if (Group == null)
                {
                    graphView.RemoveUngroupedNode(this);

                    DialogueName = target.value;

                    graphView.AddUngroupedNode(this);

                    return;
                }

                DSGroup currentGroup = Group;

                graphView.RemoveGroupedNode(this, Group);

                DialogueName = target.value;

                graphView.AddGroupedNode(this, currentGroup);
            });

            dialogueNameTextField.AddClasses(
                "ds-node__text-field",
                "ds-node__text-field__hidden",
                "ds-node__filename-text-field"
            );

            titleContainer.Insert(0, dialogueNameTextField);

            /* INPUT CONTAINER */

            Port inputPort = this.CreatePort("Dialogue Connection", Orientation.Horizontal, Direction.Input, Port.Capacity.Multi);

            inputContainer.Add(inputPort);

            /* EXTENSION CONTAINER */

            VisualElement customDataContainer = new VisualElement();

            customDataContainer.AddToClassList("ds-node__custom-data-container");

            TextField textTextFieldEN = DSElementUtility.CreateTextArea(TextEN, null, callback => TextEN = callback.newValue);
            TextField textTextFieldES = DSElementUtility.CreateTextArea(TextES, null, callback => TextES = callback.newValue);
            TextField textTextFieldCA = DSElementUtility.CreateTextArea(TextCA, null, callback => TextCA = callback.newValue);

            CreateTextBox(customDataContainer, textTextFieldEN, "English Dialogue");
            CreateTextBox(customDataContainer, textTextFieldES, "Spanish Dialogue");
            CreateTextBox(customDataContainer, textTextFieldCA, "Catalan Dialogue");

            //Audio clip
            ObjectField audioFieldEN = DSElementUtility.CreateObjectField(AudioClipEN, "AudioClipEN", typeof(AudioClip), callback => AudioClipEN = (AudioClip)callback.newValue);
            customDataContainer.Add(audioFieldEN);

            ObjectField audioFieldES = DSElementUtility.CreateObjectField(AudioClipES, "AudioClipES", typeof(AudioClip), callback => AudioClipES = (AudioClip)callback.newValue);
            customDataContainer.Add(audioFieldES);

            ObjectField audioFieldCA = DSElementUtility.CreateObjectField(AudioClipCA, "AudioClipCA", typeof(AudioClip), callback => AudioClipCA = (AudioClip)callback.newValue);
            customDataContainer.Add(audioFieldCA);

            //Animation Clip
            ObjectField animationField = DSElementUtility.CreateObjectField(AnimationClip, "AnimationClip", typeof(AnimationClip), callback => AnimationClip = (AnimationClip)callback.newValue);

            //End ID
            IntegerField eventIDField = DSElementUtility.CreateIntegerField(EventID, "Event ID", 1, callback => EventID = callback.newValue);
            customDataContainer.Add(animationField);

            //Check if any output port is connected, if not it's an ending node, then draw the End ID Property
            //if (IsEndingNode())
            customDataContainer.Add(eventIDField);

            extensionContainer.Add(customDataContainer);
        }

        private void CreateTextBox(VisualElement customDataContainer, TextField textTextField, string foldoutTitle)
        {
            Foldout textFoldout = DSElementUtility.CreateFoldout(foldoutTitle);

            textTextField.AddClasses(
                "ds-node__text-field",
                "ds-node__quote-text-field"
            );

            textFoldout.Add(textTextField);

            customDataContainer.Add(textFoldout);
        }

        public void DisconnectAllPorts()
        {
            DisconnectInputPorts();
            DisconnectOutputPorts();
        }

        private void DisconnectInputPorts()
        {
            DisconnectPorts(inputContainer);
        }

        private void DisconnectOutputPorts()
        {
            DisconnectPorts(outputContainer);
        }

        private void DisconnectPorts(VisualElement container)
        {
            foreach (Port port in container.Children())
            {
                if (!port.connected)
                {
                    continue;
                }

                graphView.DeleteElements(port.connections);
            }
        }

        public bool IsStartingNode()
        {
            Port inputPort = (Port)inputContainer.Children().First();

            return !inputPort.connected;
        }

        public bool IsEndingNode()
        {
            Port outputPort = (Port)outputContainer.Children().First();

            return !outputPort.connected;
        }

        public void SetErrorStyle(Color color)
        {
            mainContainer.style.backgroundColor = color;
        }

        public void ResetStyle()
        {
            mainContainer.style.backgroundColor = defaultBackgroundColor;
        }
    }
}