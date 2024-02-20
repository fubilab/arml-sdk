using UnityEngine;

public class AnchoredGrabbable : Grabbable
{
    protected Quaternion targetRotation;
    protected float distanceFromTarget;

    [Header("Anchored Grabbable Settings")]
    [SerializeField]
    protected bool MatchRotation = false;

    [SerializeField]
    protected float DistanceToTargetOffset = 0f; // Adjustable offset from the target hit point

    [SerializeField]
    private bool PositionAtRaycastHit = true; // Toggle for positioning at raycast hit

    private Camera mainCamera;

    public override void Grab()
    {
        distanceFromTarget = CalculateDistanceFromTarget();
        rb.useGravity = false;
        OnObjectGrabbedEvent?.Invoke();
        base.Grab();
    }

    public override void Release()
    {
        rb.useGravity = usesGravity;
        base.Release();
    }

    public override void UpdateGrabbedPosition()
    {
        if (PositionAtRaycastHit)
        {
            RePositionTargetBasedOnHit();
        }

        Vector3 targetPosition = grabTarget.position + CalculateCameraToTargetVector() * (distanceFromTarget + DistanceToTargetOffset);
        transform.position = Vector3.Lerp(rb.position, targetPosition, Time.fixedDeltaTime * lerpScale);

        if (MatchRotation)
        {
            targetRotation = grabTarget.rotation;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * lerpScale);
        }
    }

    private void RePositionTargetBasedOnHit()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        int layerMask = LayerMask.GetMask("Map");

        RaycastHit hit;
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            grabTarget.position = hit.point;
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