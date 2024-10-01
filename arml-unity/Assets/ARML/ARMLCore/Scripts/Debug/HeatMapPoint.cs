using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Represents a point on a heat map, which changes color based on the number of hits it receives.
    /// </summary>
    public class HeatMapPoint : MonoBehaviour
    {
        /// <summary>
        /// The gradient used to evaluate the color of the point based on the number of hits.
        /// </summary>
        [Tooltip("The gradient used to evaluate the color of the point based on the number of hits.")]
        public Gradient gradient;

        /// <summary>
        /// The rate at which the color changes per hit, scaled by the value division.
        /// </summary>
        [Tooltip("The rate at which the color changes per hit, scaled by the value division.")]
        public float changeRate = 0.46f;

        /// <summary>
        /// The size of the heat map point.
        /// </summary>
        [Tooltip("The size of the heat map point.")]
        public float pointSize = 0.144f;

        private int timesHit;      // Tracks the number of hits the point has received.
        private Material mat;      // The material of the heat map point.
        private float lerpValue;   // Value used for color interpolation.

        /// <summary>
        /// Initializes the heat map point by setting its material and scale.
        /// </summary>
        private void Start()
        {
            mat = GetComponent<MeshRenderer>().material;
            mat.color = gradient.Evaluate(0f);  // Set initial color to the first color in the gradient.

            transform.localScale *= pointSize;   // Scale the point to the specified size.
        }

        /// <summary>
        /// Called when the point is hit. Increases the hit count and updates the color based on the gradient.
        /// </summary>
        /// <param name="valueDivision">A divisor that affects how much the color changes based on hits.</param>
        public void PointHit(int valueDivision)
        {
            timesHit++;

            // Update the lerp value and calculate the new color based on the gradient.
            lerpValue += (changeRate / valueDivision);
            Color newColor = gradient.Evaluate(lerpValue);

            mat.color = newColor; // Set the new color for the material.
        }
    }
}
