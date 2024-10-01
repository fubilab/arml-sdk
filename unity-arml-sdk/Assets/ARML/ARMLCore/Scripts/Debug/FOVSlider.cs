using TMPro;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Controls the field of view (FOV) of the main camera and updates the corresponding UI text element.
    /// </summary>
    public class FOVSlider : MonoBehaviour
    {
        public TextMeshProUGUI fovText;

        /// <summary>
        /// Initializes the FOV text with the current FOV of the main camera at start.
        /// </summary>
        private void Start()
        {
            fovText.text = Camera.main.fieldOfView.ToString();
        }

        /// <summary>
        /// Changes the field of view of the main camera and updates the FOV text.
        /// </summary>
        /// <param name="fov">The new FOV value to be set.</param>
        public void ChangeFOV(float fov)
        {
            Camera.main.fieldOfView = fov;
            // Uncomment the following line if you want to update the FOV text immediately when the slider changes value.
            // fovText.text = Camera.main.fieldOfView.ToString();
        }

        /// <summary>
        /// Continuously updates the FOV text with the current FOV of the main camera.
        /// </summary>
        private void Update()
        {
            fovText.text = Camera.main.fieldOfView.ToString();
        }
    }
}