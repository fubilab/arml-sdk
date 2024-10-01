using UnityEngine;
using UnityEngine.Playables;

namespace ARML
{
    /// <summary>
    /// A PlayableBehaviour that sets the active state of a GameObject.
    /// </summary>
    public class SetActiveBehaviour : PlayableBehaviour
    {
        /// <summary>
        /// Defines the active state to be set on the GameObject.
        /// </summary>
        public bool setActive = true;

        /// <summary>
        /// Called each frame while the Playable is being evaluated.
        /// </summary>
        /// <param name="playable">The Playable that owns the behaviour.</param>
        /// <param name="info">Information about the current frame.</param>
        /// <param name="playerData">The object set in the playerData property, expected to be a GameObject.</param>
        /// <remarks>
        /// If the application is playing, this method sets the active state of the GameObject
        /// specified in playerData to the value of the setActive field.
        /// </remarks>
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (Application.isPlaying)
            {
                GameObject go = playerData as GameObject;

                if (go != null)
                {
                    go.SetActive(setActive);
                }
            }
        }
    }
}