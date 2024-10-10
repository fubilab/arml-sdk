using UnityEngine;

namespace ARML.Arduino
{
    /// <summary>
    /// ScriptableObject representing an Arduino animation configuration.
    /// This allows for the creation of animation settings that can be reused and adjusted in the Unity editor.
    /// </summary>
    [CreateAssetMenu(menuName = "ARML/Create New Arduino Animation", fileName = "Arduino Animation")]
    public class ArduinoAnimationSO : ScriptableObject
    {
        [Header("Color Settings")]
        [Tooltip("The solid color to use for the animation.")]
        public Color solidColor;

        [Tooltip("The color used to represent progress within the animation.")]
        public Color progressColor;

        [Tooltip("Controls the white brightness level, ranging from 0 to 255.")]
        [Range(0, 254)] public float whiteBrightness;

        [Tooltip("Controls the overall brightness of the colors, ranging from 0 to 255.")]
        [Range(0, 254)] public float overallBrightness;

        [Header("Animation Settings")]
        [Tooltip("Enables or disables snake animation mode.")]
        public bool isSnakeAnimation = false;

        [Tooltip("Sets the direction in which the animation will play. Can be forwards or backwards.")]
        public AnimationDirection animationDirection;

        [Tooltip("Defines the total number of pixels in the strip.")]
        public int totalPixelsInStrip;

        [Tooltip("Time in seconds it takes for the entire animation to loop.")]
        public float animationTime = 1f;

        [Tooltip("Sets the length of the animation in terms of the number of pixels.")]
        public int animationPixelLength = 1;

        [Tooltip("The starting pixel index for the animation.")]
        public int animationStartPixelIndex = 0;

        [Tooltip("The ending pixel index for the animation. Default is 72.")]
        public int animationEndPixelIndex = 72;

        [Tooltip("Turns off pixels not in range.")]
        public bool clearPixelsOutsideRange = false;
    }

    /// <summary>
    /// Enum representing the possible animation directions: forwards or backwards.
    /// </summary>
    public enum AnimationDirection
    {
        /// <summary>
        /// Animation progresses forwards.
        /// </summary>
        FORWARDS,

        /// <summary>
        /// Animation progresses backwards.
        /// </summary>
        BACKWARDS
    }
}
