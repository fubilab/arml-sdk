using DS.ScriptableObjects;
using DS;
using UnityEngine;
using UnityEngine.Playables;

namespace ARML
{
    /// <summary>
    /// A PlayableBehaviour for controlling the Dialogue System, can change between DialogueContainers for a DialogueSystem and initiate the dialogue
    /// </summary>
    public class DialogueSystemBehaviour : PlayableBehaviour
    {
        /// <summary>
        /// Indicates whether to change the dialogue when this behavior is triggered.
        /// </summary>
        public bool changeDialogue = true;

        /// <summary>
        /// The new dialogue container to set.
        /// </summary>
        public DSDialogueContainerSO newDialogueContainerSO;

        /// <summary>
        /// A flag to ensure this behavior is executed only once.
        /// </summary>
        private bool alreadyDone;

        /// <summary>
        /// Processes the frame of the playable.
        /// </summary>
        /// <param name="playable">The playable being processed.</param>
        /// <param name="info">Frame data information.</param>
        /// <param name="playerData">The object associated with the playable.</param>
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (Application.isPlaying)
            {
                // If the behavior has already been executed, return.
                if (alreadyDone)
                    return;

                // Get the DSDialogue component from the playerData.
                DSDialogue dsDialogue = playerData as DSDialogue;

                if (newDialogueContainerSO != null)
                {
                    // TODO: Determine how to choose the starting dialogue from the new container.
                    // You may need to iterate through grouped and ungrouped dialogues in the container
                    // to find a suitable starting dialogue.
                }

                // Check if the DSDialogue component is active and enabled.
                if (dsDialogue.isActiveAndEnabled)
                {
                    // Restart the dialogue system with the new settings.
                    dsDialogue.RestartDialogue();
                }

                // Set the alreadyDone flag to true to ensure this behavior is executed only once.
                alreadyDone = true;
            }
        }
    }
}