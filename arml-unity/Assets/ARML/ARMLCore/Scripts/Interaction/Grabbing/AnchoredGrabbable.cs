using NaughtyAttributes;
using UnityEngine;
using DG.Tweening;

namespace ARML.Interaction
{
    /// <summary>
    /// Represents an object that can be grabbed and anchored in a virtual environment, allowing for
    /// rotation and position adjustments based on raycasting.
    /// </summary>
    public class AnchoredGrabbable : Grabbable
    {
        protected Quaternion targetRotation; // Target rotation when matching the grab target's rotation.
        protected float distanceFromTarget; // Distance from the grab target.

        [Header("Anchored Grabbable Settings")]

        [Tooltip("If true, the grabbable will match the rotation of the grab target.")]
        [SerializeField]
        protected bool MatchRotation = false;

        [Tooltip("Adjustable offset from the target hit point to control distance.")]
        [SerializeField]
        protected float DistanceToTargetOffset = 0f;

        [Tooltip("Adjustable offset vector from the target hit point for precise positioning.")]
        [SerializeField]
        protected Vector3 DistanceToTargetOffsetVector;

        [Tooltip("If true, the grabbable will position itself at the raycast hit point.")]
        [SerializeField]
        private bool PositionAtRaycastHit = true;

        [Tooltip("Maximum distance the grabbable can be from the camera.")]
        [SerializeField]
        private float maxDistanceFromCamera = 10f;

        [Tooltip("Scale factor to apply when the object is grabbed.")]
        [SerializeField]
        private float scaleWhenGrabbed = 1f;

        private Vector3 originalScale; // Original scale of the object.
        private Camera mainCamera; // Reference to the main camera.

        protected override void Awake()
        {
            base.Awake();
            originalScale = transform.localScale; // Store the original scale for later use.
        }

        /// <summary>
        /// Called when the object is grabbed. Changes the object's scale and prepares for movement.
        /// </summary>
        public override void Grab()
        {
            base.Grab();
            if (mainCamera == null)
            {
                mainCamera = Camera.main; // Get the main camera if not already referenced.
            }
            distanceFromTarget = CalculateDistanceFromTarget(); // Calculate initial distance from the target.
            rb.useGravity = false; // Disable gravity when grabbing.
            transform.DOScale(transform.localScale * scaleWhenGrabbed, 0.5f); // Animate scaling.
            OnObjectGrabbedEvent?.Invoke(); // Trigger the grab event.
        }

        /// <summary>
        /// Called when the object is placed back in the scene. Resets the scale to original.
        /// </summary>
        public override void Place()
        {
            base.Place();
            transform.DOScale(originalScale, 0.5f); // Animate back to original scale.
        }

        /// <summary>
        /// Called when the object is released. Resets the rigidbody's gravity setting.
        /// </summary>
        public override void Release()
        {
            rb.useGravity = usesGravity; // Restore gravity settings.
            base.Release();
        }

        /// <summary>
        /// Updates the position of the grabbed object based on its grab target.
        /// </summary>
        public override void UpdateGrabbedPosition()
        {
            if (PositionAtRaycastHit)
            {
                RePositionTargetBasedOnHit(); // Update position based on raycast hit.
            }
            else
            {
                // Use the standard method to update position if not relying on raycast hits.
                Vector3 targetPosition = grabTarget.position + CalculateCameraToTargetVector() * (distanceFromTarget + DistanceToTargetOffset)
                    + transform.InverseTransformVector(mainCamera.transform.position);
                transform.position = Vector3.Lerp(rb.position, targetPosition, Time.fixedDeltaTime * lerpScale);
            }

            if (MatchRotation)
            {
                targetRotation = grabTarget.rotation; // Get target rotation.
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * lerpScale); // Smoothly rotate towards target.
            }
        }

        /// <summary>
        /// Repositions the grab target based on the raycast hit point from the camera.
        /// </summary>
        private void RePositionTargetBasedOnHit()
        {
            RaycastHit hit;
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            int layerMask = LayerMask.GetMask("Map");

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                Vector3 hitPoint = hit.point;
                Vector3 directionToHit = hitPoint - mainCamera.transform.position;
                float distanceToHit = directionToHit.magnitude;

                // If the hit is further than maxDistanceFromCamera, calculate a new target point within the limit
                if (distanceToHit > maxDistanceFromCamera)
                {
                    hitPoint = mainCamera.transform.position + directionToHit.normalized * maxDistanceFromCamera;
                    distanceFromTarget = maxDistanceFromCamera;
                }
                else
                {
                    distanceFromTarget = distanceToHit;
                }

                // Set the grabTarget position to the calculated hitPoint
                grabTarget.position = hitPoint;
            }

            // Update the object's position based on the newly calculated or existing grabTarget position
            Vector3 targetPosition = grabTarget.position + CalculateCameraToTargetVector() * DistanceToTargetOffset
                + DistanceToTargetOffsetVector;
            transform.position = Vector3.Lerp(rb.position, targetPosition, Time.fixedDeltaTime * lerpScale);
        }

        /// <summary>
        /// Calculates the distance from the grab target.
        /// </summary>
        /// <returns>The distance from the target.</returns>
        private float CalculateDistanceFromTarget()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            Vector3 targetToThis = transform.position - grabTarget.position;
            Vector3 camToTarget = CalculateCameraToTargetVector();
            return Vector3.Dot(targetToThis, camToTarget.normalized);
        }

        /// <summary>
        /// Calculates the vector from the camera to the target.
        /// </summary>
        /// <returns>The normalized vector from the camera to the target.</returns>
        private Vector3 CalculateCameraToTargetVector()
        {
            return (grabTarget.position - mainCamera.transform.position).normalized;
        }

        [Button("Spawn Placement Target")]
        private void SpawnPlacementTarget()
        {
            string name = $"{gameObject.name}_PlacementTarget";
            if (!GameObject.Find(name))
            {
                // Give it the right name, model, set up name filtering etc.
                GameObject placementTarget = new GameObject(name, typeof(PlacementTarget));
                placementTarget.GetComponent<PlacementTarget>().AutoSetUp(this, model);
            }
            else
            {
                // It already exists, notify user.
                Debug.LogError($"A PlacementTarget with name {name} already exists. Delete or rename it if you want to create another one");
            }
        }
    }
}
