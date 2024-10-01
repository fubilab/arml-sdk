using System;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Manages the interaction type within the game and broadcasts changes to listeners.
    /// </summary>
    public class InteractionTypeController : MonoBehaviour
    {
        InteractionType interactionType;

        /// <summary>
        /// Event triggered when the interaction type is changed.
        /// </summary>
        public Action<InteractionType> OnInteractionTypeChanged;

        #region Singleton
        public static InteractionTypeController Instance { get; private set; }

        /// <summary>
        /// Ensures that only one instance of InteractionTypeController exists.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }
        #endregion

        /// <summary>
        /// Changes the current interaction type and invokes the OnInteractionTypeChanged event.
        /// </summary>
        /// <param name="isButton">If true, sets the interaction type to BUTTON, otherwise to DWELL.</param>
        public void ChangeInteractionType(bool isButton)
        {
            if (isButton)
                interactionType = InteractionType.BUTTON;
            else
                interactionType = InteractionType.DWELL;

            OnInteractionTypeChanged?.Invoke(interactionType);
            Debug.Log("Changed interaction type to " + interactionType.ToString());
        }
    }

    /// <summary>
    /// Defines possible interaction types.
    /// </summary>
    public enum InteractionType
    {
        DWELL,
        BUTTON,
        VOICE
    }
}