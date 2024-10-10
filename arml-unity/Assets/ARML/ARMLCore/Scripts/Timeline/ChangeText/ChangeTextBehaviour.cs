using TMPro;
using UnityEngine;
using UnityEngine.Playables;

namespace ARML.Timeline
{
    /// <summary>
    /// A custom PlayableBehaviour to change the text of a TextMeshProUGUI component during a Timeline clip.
    /// Allows for setting the text to a specific value and optionally resetting it at the end of the clip.
    /// </summary>
    public class ChangeTextBehaviour : PlayableBehaviour
    {
        public string textToChangeTo = null;
        public bool setEmptyAtTheEnd = true;

        private string originalText;
        private TextMeshProUGUI tMPro;

        /// <summary>
        /// Called each frame while the Timeline clip is playing. Updates the text of the TextMeshProUGUI component.
        /// </summary>
        /// <param name="playable">The Playable which owns this Behaviour.</param>
        /// <param name="info">Frame information for the current frame.</param>
        /// <param name="playerData">Player data, expected to be a TextMeshProUGUI component.</param>
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            tMPro = playerData as TextMeshProUGUI;

            if (tMPro == null) return;

            if (string.IsNullOrEmpty(originalText))
            {
                originalText = tMPro.text;
            }

            tMPro.text = textToChangeTo;
        }

        /// <summary>
        /// Called when the Timeline clip is paused or finishes playing. Resets the text if specified.
        /// </summary>
        /// <param name="playable">The Playable which owns this Behaviour.</param>
        /// <param name="info">Frame information for the current frame.</param>
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // Only execute in Play mode
            if (Application.isPlaying)
            {
                var duration = playable.GetDuration();
                var time = playable.GetTime();
                var count = time + info.deltaTime;

                if ((info.effectivePlayState == PlayState.Paused && count > duration) || Mathf.Approximately((float)time, (float)duration))
                {
                    // Execute your finishing logic here:
                    if (setEmptyAtTheEnd && tMPro != null)
                    {
                        tMPro.text = "";
                    }
                }
            }
        }
    }
}