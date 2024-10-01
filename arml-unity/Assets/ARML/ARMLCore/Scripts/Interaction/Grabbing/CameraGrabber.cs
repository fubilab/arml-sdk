using DS;
using System.Collections.Generic;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Handles grabbing and releasing grabbable objects in a 3D space,
    /// managing interactions with the camera and user inputs.
    /// </summary>
    public class CameraGrabber : MonoBehaviour
    {
        [SerializeField]
        private Transform target; // Grabbables will try to attach themselves to this point and not the object root.

        [SerializeField]
        private Vector3 targetRotationOffset; // Rotation offset for the target to which grabbables will attach.

        public Grabbable grabbedObject; // Currently grabbed object.
        private Grabbable pendingGrabbedObject; // Object that is about to be grabbed.
        private Grabbable lastGrabbedObject; // The last grabbed object.
        private bool canGrabLastGrabbedObject; // Flag to determine if the last grabbed object can be re-grabbed.

        [Tooltip("Minimum separation distance between the placed object and the hand to be able to grab it again right after placing it (to prevent instant grabbing right after placement).")]
        public float minimumSquaredDistanceToRegrabPlacedObject = 0.1f;

        private List<Grabbable> grabbablesInsideTrigger = new List<Grabbable>(); // List of grabbables inside the trigger.
        public Camera cam { get; private set; } // Reference to the main camera.

        [Header("Audio and feedback")]
        public ActionFeedback feedback; // Feedback system for audio and visual effects.

        private float targetDistanceToCam; // Distance to the camera for positioning.

        private DebugCanvasController debugCanvasController; // Controller for the debug canvas.

        public void Start()
        {
            cam = Camera.main;
            target.transform.parent = transform;
            targetDistanceToCam = 1f;
            target.transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z + targetDistanceToCam);
            target.transform.localEulerAngles = targetRotationOffset;

            debugCanvasController = FindObjectOfType<DebugCanvasController>();
        }

        /// <summary>
        /// Grabs the object that is pending to be grabbed and sets its position.
        /// </summary>
        private void GrabObject()
        {
            // Set target to grabbed object distance
            targetDistanceToCam = Vector3.Distance(pendingGrabbedObject.transform.position, transform.position);
            target.localPosition = new Vector3(0, 0, targetDistanceToCam); // Update the target's position.

            ForceGrabObject(pendingGrabbedObject); // Force grab the object.
            grabbedObject.iTimer.OnFinishInteraction -= GrabObject; // Unsubscribe from interaction events.
            pendingGrabbedObject = null; // Clear pending object.
        }

        /// <summary>
        /// Forces grabbing of a specified grabbable object.
        /// </summary>
        /// <param name="other">The grabbable object to be grabbed.</param>
        public void ForceGrabObject(Grabbable other)
        {
            grabbedObject = other; // Set the grabbed object.
            grabbedObject.OnPlace += ForceReleaseObject; // Subscribe to place event.
            feedback?.PlayRandomTriggerFeedback(); // Play feedback sound.
            other.Grab(); // Call the Grab method on the object.
        }

        /// <summary>
        /// Releases the currently grabbed object.
        /// </summary>
        private void ReleaseObject()
        {
            if (grabbedObject != null) // Check if an object is currently grabbed.
                grabbedObject.Release(); // Release the object.
            ForceReleaseObject(); // Call force release to clear references.
        }

        /// <summary>
        /// Forces the release of the currently grabbed object.
        /// </summary>
        public void ForceReleaseObject()
        {
            // Clear the placement subscription to avoid bugs.
            if (grabbedObject != null)
                grabbedObject.OnPlace -= ForceReleaseObject; // Unsubscribe from place event.

            pendingGrabbedObject = null; // Clear pending object.
            lastGrabbedObject = grabbedObject; // Set last grabbed object.
            grabbedObject = null; // Clear the currently grabbed object.

            canGrabLastGrabbedObject = false; // Reset re-grab flag.
        }

        private void FixedUpdate()
        {
            if (grabbedObject != null)
            {
                grabbedObject.UpdateGrabbedPosition(); // Update the position of the grabbed object.
            }

            if (!canGrabLastGrabbedObject)
                CheckIfCanGrabLastGrabbedObject(); // Check if the last object can be grabbed again.

            if (pendingGrabbedObject == null)
                return;

            grabbablesInsideTrigger.Clear(); // Clear the list for next frame.
        }

        private void OnTriggerExit(Collider other)
        {
            if (pendingGrabbedObject != null && (other == pendingGrabbedObject.placeable.grabCollider
                || other == pendingGrabbedObject.grabbableCollider))
                ReleasePendingGrabbedObject(); // Release the pending object if it exits the trigger.

            DSDialogue d = other.GetComponent<DSDialogue>(); // Check for dialogue component.

            if (d == null || !d.enabled) return;

            d.StopTalkingAttempt(); // Stop talking if applicable.
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleTalking(other); // Check if any dialogue starts.

            HandleGrabbable(other); // Handle any grabbable object entering the trigger.
        }

        private static void HandleTalking(Collider other)
        {
            DSDialogue d = other.GetComponent<DSDialogue>();

            if (d == null || !d.enabled) return;

            d.StartTalkingAttempt(); // Start talking attempt if applicable.
        }

        private void HandleGrabbable(Collider other)
        {
            Grabbable g = other.GetComponent<Grabbable>(); // Attempt to get the grabbable component.
            if (g == null)
            {
                g = other.GetComponentInParent<Grabbable>();
                if (g == null)
                    return; // Return if no grabbable found.
            }

            // Return if component not active.
            if (!g.enabled) return;

            if (grabbedObject == null && pendingGrabbedObject == null)
            {
                if (g == lastGrabbedObject && !canGrabLastGrabbedObject)
                    return; // If it's the last grabbed object and can't re-grab, exit.

                if (g.StartGrabbingAttempt(target)) // Start the grabbing attempt.
                {
                    g.iTimer.OnFinishInteraction += GrabObject; // Subscribe to the timer end event.
                    pendingGrabbedObject = g; // Set the pending grabbed object.
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            Grabbable g = other.GetComponent<Grabbable>(); // Attempt to get the grabbable component.
            if (g == null)
            {
                g = other.GetComponentInParent<Grabbable>();
                if (g == null)
                    return; // Return if no grabbable found.
            }

            grabbablesInsideTrigger.Add(g); // Add the grabbable to the list.

            if (grabbedObject == null)
            {
                if (g == lastGrabbedObject && !canGrabLastGrabbedObject)
                    return; // If it's the last grabbed object and can't re-grab, exit.
                g.GrabbingUpdate(); // Update the grabbable's grabbing state.
            }
        }

        private void ReleasePendingGrabbedObject()
        {
            pendingGrabbedObject.iTimer.OnFinishInteraction -= GrabObject; // Unsubscribe from interaction event.
            pendingGrabbedObject.StopGrabbingAttempt(); // Stop the grabbing attempt.
            pendingGrabbedObject = null; // Clear pending object.
        }

        /// <summary>
        /// Checks if the last grabbed object can be re-grabbed based on its distance.
        /// </summary>
        public void CheckIfCanGrabLastGrabbedObject()
        {
            if (!canGrabLastGrabbedObject)
            {
                if (lastGrabbedObject == null)
                    canGrabLastGrabbedObject = true; // If there's no last object, allow re-grabbing.
                else
                {
                    Vector3 projection = Vector3.ProjectOnPlane(this.transform.position - lastGrabbedObject.transform.position, cam.transform.forward);
                    canGrabLastGrabbedObject = projection.sqrMagnitude > minimumSquaredDistanceToRegrabPlacedObject; // Check distance.
                }
            }
        }

        /// <summary>
        /// Clears the currently grabbed or pending object.
        /// </summary>
        public void ClearHand()
        {
            if (grabbedObject != null)
                ReleaseObject(); // Release the currently grabbed object.
            else if (pendingGrabbedObject != null)
                ReleasePendingGrabbedObject(); // Release the pending object.
        }

        private void OnDisable()
        {
            ClearHand(); 
        }

        private void OnDestroy()
        {
            ClearHand(); 
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red; // Set gizmo color to red.
                Gizmos.DrawRay(transform.position, transform.forward * 3); // Draw a ray in front of the object.
                if (target != null)
                    Gizmos.DrawWireSphere(target.position, 0.5f); // Draw a wire sphere around the target.
            }
        }
    }
}
