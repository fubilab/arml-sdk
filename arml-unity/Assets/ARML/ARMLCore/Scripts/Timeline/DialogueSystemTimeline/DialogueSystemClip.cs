using ARML.DS.ScriptableObjects;
using UnityEngine;
using UnityEngine.Playables;

namespace ARML.Timeline
{
    /// <summary>
    /// A PlayableAsset Clip for controlling the Dialogue System.
    /// </summary>
    public class DialogueSystemClip : PlayableAsset
    {
        /// <summary>
        /// Indicates whether to change the dialogue when this clip is triggered.
        /// </summary>
        public bool changeDialogue = false;

        /// <summary>
        /// The new dialogue container to set.
        /// </summary>
        public DSDialogueContainerSO newDialogueContainerSO;

        /// <summary>
        /// Creates a playable instance of the DialogueSystemBehaviour.
        /// </summary>
        /// <param name="graph">The PlayableGraph in which the playable will exist.</param>
        /// <param name="owner">The GameObject to which the playable will be attached.</param>
        /// <returns>A playable instance of DialogueSystemBehaviour.</returns>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<DialogueSystemBehaviour>.Create(graph);

            var behaviour = playable.GetBehaviour();
            behaviour.changeDialogue = changeDialogue;
            behaviour.newDialogueContainerSO = newDialogueContainerSO;

            return playable;
        }
    }
}