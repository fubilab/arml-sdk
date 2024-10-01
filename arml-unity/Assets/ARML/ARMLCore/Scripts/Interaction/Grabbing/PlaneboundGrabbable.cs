using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Represents a grabbable object whose movement is constrained to a specific plane.
    /// </summary>
    public class PlaneboundGrabbable : Grabbable
    {
        protected Plane constraintPlane;

        /// <summary>
        /// Initializes the PlaneboundGrabbable by setting up the constraint plane.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            constraintPlane = new Plane(this.transform.forward, this.transform.position);
        }

        /// <summary>
        /// Handles the logic when the object is grabbed, disabling gravity and allowing kinematic movement.
        /// </summary>
        public override void Grab()
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            base.Grab();
        }

        /// <summary>
        /// Handles the logic when the object is released, restoring the original gravity settings.
        /// </summary>
        public override void Release()
        {
            rb.useGravity = usesGravity;
            base.Release();
        }

        /// <summary>
        /// Updates the position of the grabbed object, constraining its movement to the defined plane.
        /// </summary>
        public override void UpdateGrabbedPosition()
        {
            Ray ray = new Ray(cam.transform.position, CalculateCameraToTargetVector());
            float distance;
            if (constraintPlane.Raycast(ray, out distance))
            {
                Vector3 targetPosition = cam.transform.position + ray.direction * distance;
                this.rb.MovePosition(Vector3.Lerp(this.rb.position, targetPosition, Time.fixedDeltaTime * lerpScale));
            }
        }
    }
}