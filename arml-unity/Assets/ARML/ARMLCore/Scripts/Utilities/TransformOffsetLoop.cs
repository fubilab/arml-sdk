using System.Collections;
using UnityEngine;

/// <summary>
/// Animates a GameObject's position, rotation, and scale in a loop based on specified offsets.
/// Offers different looping modes including PingPong (back and forth) and Reset (return to start).
/// </summary>
public class TransformOffsetLoop : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] Vector3 positionOffset = new Vector3(0f, 0f, 0.0f);
    private Vector3 originalPosition;
    private Vector3 targetPosition;

    [Header("Rotation")]
    [SerializeField] Vector3 rotationOffset = new Vector3(0f, 0f, 0.0f);
    private Vector3 originalRotation;
    private Vector3 targetRotation;

    [Header("Scale")]
    [SerializeField] Vector3 scaleOffset = new Vector3(0f, 0f, 0.0f);
    private Vector3 originalScale;
    private Vector3 targetScale;

    [Header("Lerp Settings")]
    [SerializeField] float lerpDuration = 0.5f;
    [SerializeField] LoopMode loopMode = LoopMode.DontLoop;

    private float timeElapsed;
    private float lerpFactor;

    private enum LoopMode
    {
        DontLoop,
        PingPong,
        Reset
    }

    /// <summary>
    /// Initializes the original and target transformations based on the specified offsets.
    /// </summary>
    private void Start()
    {
        originalPosition = transform.position;
        targetPosition = originalPosition + positionOffset;

        originalRotation = transform.eulerAngles;
        targetRotation = originalRotation + rotationOffset;

        originalScale = transform.localScale;
        targetScale = originalScale + scaleOffset;
    }

    /// <summary>
    /// Updates the GameObject's position, rotation, and scale by lerping between the original and target values.
    /// Handles looping logic based on the selected loop mode.
    /// </summary>
    private void Update()
    {
        // Increment the time elapsed
        timeElapsed += Time.deltaTime;

        // Calculate lerp factor based on time elapsed and lerp duration
        lerpFactor = Mathf.Clamp01(timeElapsed / lerpDuration);

        // Lerp between the original position and the target position
        Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, lerpFactor);
        Vector3 newRotation = Vector3.Lerp(originalRotation, targetRotation, lerpFactor);
        Vector3 newScale = Vector3.Lerp(originalScale, targetScale, lerpFactor);

        // Update the GameObject's position
        transform.position = newPosition;
        transform.eulerAngles = newRotation;
        transform.localScale = newScale;

        // Check if the lerp is complete
        if (lerpFactor >= 1.0f && loopMode != LoopMode.DontLoop)
        {
            // Reset the time elapsed
            timeElapsed = 0f;

            if (loopMode == LoopMode.PingPong)
            {
                // Swap the target and original positions
                Vector3 tempPosition = targetPosition;
                targetPosition = originalPosition;
                originalPosition = tempPosition;

                Vector3 tempRotation = targetRotation;
                targetRotation = originalRotation;
                originalRotation = tempRotation;

                Vector3 tempScale = targetScale;
                targetScale = originalScale;
                originalScale = tempScale;

                return;
            }

            if (loopMode == LoopMode.Reset)
            {
                //Reset to original position
                transform.position = originalPosition;
                transform.eulerAngles = originalRotation;
                transform.localScale = originalScale;
            }                
        }
    }
}