using DG.Tweening;
using UltEvents;
using UnityEngine;

/// <summary>
/// Animates the movement, rotation, and scaling of the GameObject to a specified target using DOTween.
/// The animation includes options for duration, easing, and randomization of position offsets.
/// </summary>
public class MoveTransformToTarget : MonoBehaviour
{
    [Header("Transform Target")]
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField] bool moveToTargetPosition = true;
    [SerializeField] private float duration = 2f;
    [SerializeField] private Ease easeCurve = Ease.Linear;
    [SerializeField] private bool doOnStart;
    [Tooltip("Applies this offset to the final position")]
    [SerializeField] private Vector3 targetOffsetAmount;
    [Tooltip("Applies offset above as min (-) and max (+) random range")]
    [SerializeField] private bool randomizeOffset;

    [Header("Rotation")]
    [SerializeField] bool moveToTargetRotation;
    [Tooltip("Rotation amount over the duration. Set to Vector3.zero for no rotation.")]
    [SerializeField] private Vector3 rotationAmount = new Vector3(0, 360, 0);

    [Header("Scale")]
    [Tooltip("Final scale at the end of movement. Set to Vector3.one for no scaling.")]
    [SerializeField] private Vector3 scaleMultiplier = Vector3.one;
    private Vector3 initialScale;

    [Header("Event")]
    [SerializeField] UltEvent OnStartMoveEvent;
    [SerializeField] UltEvent OnFinishMoveEvent;

    private bool currentlyMoving;

    /// <summary>
    /// Initializes the component and triggers movement if set to do so at start.
    /// </summary>
    void Start()
    {
        initialScale = transform.localScale;

        if (doOnStart)
            MoveToTarget();
    }

    /// <summary>
    /// Initiates the movement of the GameObject towards the target, including optional rotation and scaling effects.
    /// </summary>
    public void MoveToTarget()
    {
        if (!this.enabled)
            return;

        if (currentlyMoving) return;

        OnStartMoveEvent?.Invoke();

        currentlyMoving = true;

        Vector3 targetPosition = target.position + CalculateOffset();
        Vector3 targetRotation = target.eulerAngles;

        // Move the object to target position
        if (moveToTargetPosition)
            transform.DOMove(targetPosition, duration).SetEase(easeCurve);

        if (moveToTargetRotation)
            transform.DORotate(targetRotation, duration, RotateMode.FastBeyond360);

        //TODO Rethink this
        // Rotate the object if rotationAmount is not Vector3.zero
        //if (rotationAmount != Vector3.zero)
        //    transform.DORotate(rotationAmount, duration, RotateMode.LocalAxisAdd);

        // Scale the object if finalScale is not Vector3.one
        if (scaleMultiplier != Vector3.one)
            transform.DOScale(Vector3.Scale(initialScale, scaleMultiplier), duration).SetEase(easeCurve);

        Invoke(nameof(InvokeOnFinishMoveEvent), duration);
    }

    private void InvokeOnFinishMoveEvent()
    {
        currentlyMoving = false;
        OnFinishMoveEvent?.Invoke();
    }

    public void SetTransformTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Calculates the offset to be applied to the target's position, either as a fixed value or randomized within a range.
    /// </summary>
    /// <returns>The offset as a Vector3.</returns>
    private Vector3 CalculateOffset()
    {
        if (targetOffsetAmount == Vector3.zero)
            return Vector3.zero;

        if (randomizeOffset)
        {
            return new Vector3(
                Random.Range(-targetOffsetAmount.x, targetOffsetAmount.x),
                Random.Range(-targetOffsetAmount.y, targetOffsetAmount.y),
                Random.Range(-targetOffsetAmount.z, targetOffsetAmount.z)
            );
        }
        else
        {
            return targetOffsetAmount;
        }
    }
}
