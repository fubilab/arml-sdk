using UnityEngine;

namespace ARML.Interaction
{
    /// <summary>
    /// Manages interactions with a Rigidbody component, allowing forces to be applied relative to the camera's direction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyInteraction : MonoBehaviour
    {
        [SerializeField] private bool printVelocity;

        Rigidbody rb;
        Camera cam;

        private Vector3 previousPosition;
        private Vector3 kinematicVelocity;

        /// <summary>
        /// Initializes the script, getting the Rigidbody and main camera components.
        /// </summary>
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            cam = Camera.main;
        }

        /// <summary>
        /// Ensures the Rigidbody component is referenced properly. Called when the script is loaded or a value is changed in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            rb = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Applies a force to the Rigidbody in the direction of the camera's forward vector.
        /// </summary>
        /// <param name="force">The magnitude of the force to apply.</param>
        public void AddForce(float force)
        {
            if (!cam)
                cam = Camera.main;

            Vector3 forceVector = cam.transform.forward * force;
            rb.AddForce(forceVector);
        }

        /// <summary>
        /// Optionally prints the current velocity of the Rigidbody each frame.
        /// </summary>
        private void FixedUpdate()
        {
            kinematicVelocity = (transform.position - previousPosition) / Time.deltaTime;
            previousPosition = transform.position;

            if (printVelocity)
                Debug.Log($"{gameObject.name} has velocity {kinematicVelocity}");
        }

        public Vector3 GetKinematicVelocity()
        {
            return kinematicVelocity;
        }
    }
}