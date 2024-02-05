using UnityEngine;

/// <summary>
/// Represents an object that can be grabbed and anchored at a specific distance from a target, with optional rotation matching.
/// </summary>
public class AnchoredGrabbable : Grabbable
{
    protected Quaternion targetRotation;
    protected float distanceFromTarget;

    [field: Header("Anchored Grabbable")]
    [field: SerializeField]
    protected bool MatchRotation { get; private set; } = false;
    [field: SerializeField]
    public float DistanceToTargetOffset { get; private set; } = 0f;

    /// <summary>
    /// Grabs the object, setting its position and rotation relative to the target.
    /// </summary>
    public override void Grab()
    {
        distanceFromTarget = CalculateDistanceFromTarget();
        rb.useGravity = false;
        OnObjectGrabbedEvent?.Invoke();
        base.Grab();
    }

    /// <summary>
    /// Releases the object, restoring its original gravity setting.
    /// </summary>
    public override void Release()
    {
        rb.useGravity = usesGravity;
        base.Release();
    }

    /// <summary>
    /// Updates the position and rotation of the grabbed object each frame.
    /// </summary>
    public override void UpdateGrabbedPosition()
    {
        Vector3 targetPosition = grabTarget.position + CalculateCameraToTargetVector() * (distanceFromTarget + DistanceToTargetOffset);
        targetRotation = grabTarget.rotation;

        transform.position = Vector3.Lerp(rb.position, targetPosition, Time.fixedDeltaTime * lerpScale);
        if (MatchRotation)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * lerpScale);
        }
    }

    /// <summary>
    /// Calculates the distance from the grab target.
    /// </summary>
    /// <returns>The distance from the target.</returns>
    private float CalculateDistanceFromTarget()
    {
        Vector3 targetToThis = transform.position - grabTarget.position;
        Vector3 camToTarget = CalculateCameraToTargetVector();
        return Vector3.Dot(targetToThis, camToTarget);
    }
}
