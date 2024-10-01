using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Manages the visibility of a MeshRenderer component. Mainly used for toggling a scanned map on/off.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ToggleMapRenderer : MonoBehaviour
    {
        private MeshRenderer meshRenderer; // The MeshRenderer component attached to this GameObject.

        /// <summary>
        /// Initializes the MeshRenderer and sets its visibility based on GameController settings.
        /// </summary>
        private void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();

            // Set the initial visibility of the MeshRenderer based on GameController settings.
            if (GameController.Instance.displayScanAtStart)
                meshRenderer.enabled = true;

#if !UNITY_EDITOR
            // Disable the MeshRenderer if not in the Unity Editor.
            meshRenderer.enabled = false;
#endif
        }

        /// <summary>
        /// Toggles the visibility of the MeshRenderer.
        /// </summary>
        public void Toggle()
        {
            meshRenderer.enabled = !meshRenderer.enabled;
        }

        /// <summary>
        /// Sets the visibility of the MeshRenderer to the specified state.
        /// </summary>
        /// <param name="state">True to enable the renderer, false to disable it.</param>
        public void SetRenderer(bool state)
        {
            meshRenderer.enabled = state;
        }
    }
}
