using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ARML.Network
{
    /// <summary>
    /// Manages the post-processing effects in the scene, allowing dynamic changes to visual effects.
    /// </summary>
    public class PostProcessingController : MonoBehaviour
    {
        /// <summary>
        /// The Volume component containing the post-processing settings.
        /// </summary>
        private Volume volume;

        /// <summary>
        /// The Vignette effect within the post-processing volume.
        /// </summary>
        private Vignette vignette;

        /// <summary>
        /// The Color Adjustments effect within the post-processing volume.
        /// </summary>
        private ColorAdjustments colorAdjustments;

        /// <summary>
        /// The Depth of Field effect within the post-processing volume.
        /// </summary>
        private DepthOfField depthOfField;

        /// <summary>
        /// Action event triggered when post-processing settings are changed.
        /// </summary>
        public static Action<PostProcessingConfig, GameObject> OnPostProcessingChanged;

        private void Start()
        {
            volume = GetComponent<Volume>();
            volume.profile.TryGet(out vignette);
            volume.profile.TryGet(out colorAdjustments);
            volume.profile.TryGet(out depthOfField);
        }

        /// <summary>
        /// Toggles the vignette effect on or off.
        /// </summary>
        /// <param name="toggle">True to enable the vignette effect, false to disable it.</param>
        public void OnToggleVignette(bool toggle)
        {
            vignette.active = toggle;
            OnPostProcessingChanged?.Invoke(GetPostProcessingConfig(), this.gameObject);
        }

        /// <summary>
        /// Changes the contrast value of the color adjustments.
        /// </summary>
        /// <param name="value">The new contrast value.</param>
        public void OnChangeValueContrast(float value)
        {
            colorAdjustments.contrast.value = value * 100;
            OnPostProcessingChanged?.Invoke(GetPostProcessingConfig(), this.gameObject);
        }

        /// <summary>
        /// Changes the focal length value in the Depth of Field effect.
        /// </summary>
        /// <param name="value">The new focal length value.</param>
        public void ChangeDoFFocalLength(float value)
        {
            if (depthOfField != null)
                depthOfField.focalLength.value = value;
            //TODO: No need to send this through network or invoke event for now
        }

        /// <summary>
        /// Sets the post-processing configuration based on a given config object.
        /// </summary>
        /// <param name="config">The post-processing configuration to apply.</param>
        public void SetPostProcessingConfig(PostProcessingConfig config)
        {
            volume.enabled = config.postProcessingOn;
            vignette.active = config.vignetteOn;
            colorAdjustments.contrast.value = config.contrastAmount;
        }

        /// <summary>
        /// Retrieves the current post-processing configuration.
        /// </summary>
        /// <returns>The current post-processing configuration.</returns>
        public PostProcessingConfig GetPostProcessingConfig()
        {
            PostProcessingConfig config = new PostProcessingConfig();
            config.postProcessingOn = volume.enabled;
            config.vignetteOn = vignette.active;
            config.contrastAmount = colorAdjustments.contrast.value;
            return config;
        }
    }
}