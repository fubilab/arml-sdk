using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ARML.DS.Elements
{
    using Data.Save;
    using Enumerations;
    using Utilities;
    using Windows;

    public class DSMultipleChoiceNode : DSNode
    {
        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 position)
        {
            base.Initialize(nodeName, dsGraphView, position);

            DialogueType = DSDialogueType.MultipleChoice;

            DSChoiceSaveData choiceData = new DSChoiceSaveData()
            {
                TextEN = "English",
                TextES = "Spanish",
                TextCA = "Catalan"
            };

            Choices.Add(choiceData);
        }

        public override void Draw()
        {
            base.Draw();

            /* MAIN CONTAINER */

            Button addChoiceButton = DSElementUtility.CreateButton("Add Choice", () =>
            {
                DSChoiceSaveData choiceData = new DSChoiceSaveData()
                {
                    TextEN = "English",
                    TextES = "Spanish",
                    TextCA = "Catalan"
                };

                Choices.Add(choiceData);

                Port choicePort = CreateChoicePort(choiceData);

                outputContainer.Add(choicePort);
            });

            addChoiceButton.AddToClassList("ds-node__button");

            mainContainer.Insert(1, addChoiceButton);

            /* OUTPUT CONTAINER */

            foreach (DSChoiceSaveData choice in Choices)
            {
                Port choicePort = CreateChoicePort(choice);

                outputContainer.Add(choicePort);
            }

            RefreshExpandedState();
        }

        private Port CreateChoicePort(object userData)
        {
            Port choicePort = this.CreatePort();

            choicePort.userData = userData;

            DSChoiceSaveData choiceData = (DSChoiceSaveData) userData;

            Button deleteChoiceButton = DSElementUtility.CreateButton("X", () =>
            {
                if (Choices.Count == 1)
                {
                    return;
                }

                if (choicePort.connected)
                {
                    graphView.DeleteElements(choicePort.connections);
                }

                Choices.Remove(choiceData);

                graphView.RemoveElement(choicePort);
            });

            deleteChoiceButton.AddToClassList("ds-node__button");

            TextField choiceTextFieldEN = DSElementUtility.CreateTextField(choiceData.TextEN, null, callback =>
            {
                choiceData.TextEN = callback.newValue;
            });

            TextField choiceTextFieldES = DSElementUtility.CreateTextField(choiceData.TextES, null, callback =>
            {
                choiceData.TextES = callback.newValue;
            });

            TextField choiceTextFieldCA = DSElementUtility.CreateTextField(choiceData.TextCA, null, callback =>
            {
                choiceData.TextCA = callback.newValue;
            });

            choiceTextFieldEN.AddClasses(
                "ds-node__text-field",
                "ds-node__text-field__hidden",
                "ds-node__choice-text-field"
            );

            choiceTextFieldES.AddClasses(
                "ds-node__text-field",
                "ds-node__text-field__hidden",
                "ds-node__choice-text-field"
            );

            choiceTextFieldCA.AddClasses(
                "ds-node__text-field",
                "ds-node__text-field__hidden",
                "ds-node__choice-text-field"
            );


            //Add Choice Text in inverted order
            choicePort.Add(choiceTextFieldCA);
            choicePort.Add(choiceTextFieldES);
            choicePort.Add(choiceTextFieldEN);

            choicePort.Add(deleteChoiceButton);

            return choicePort;
        }
    }
}