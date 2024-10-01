using System.Collections.Generic;
using UltEvents;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Handles collision detection and triggers events based on specified conditions.
    /// </summary>
    public class CollisionCheck : MonoBehaviour
    {
        enum CheckType
        {
            OnEnter,
            OnExit,
            OnStay
        }

        [Header("Collision Settings")]
        [Tooltip("Number of collision checks needed to trigger the event.")]
        [SerializeField] private int numberOfChecksToTrigger;

        [Tooltip("Allows the same object to trigger a collision more than once.")]
        [SerializeField] private bool allowSameObjectRecollision;

        [Tooltip("Resets the collision check count once the trigger condition is met.")]
        [SerializeField] private bool resetCheckOnceMet;

        [Tooltip("Deactivates the interactable component of the collided object.")]
        [SerializeField] private bool deactivateCollidedInteractables;

        [Tooltip("Triggers action feedback on collision.")]
        [SerializeField] private bool triggerActionFeedback;

        [Tooltip("Specifies the required velocity vector for collision to be considered.")]
        [SerializeField] private Vector3 requiredVelocityVector;

        [Tooltip("Specifies if the required velocity is from a kinematic object (typically a grabbed Grabbable) - therefore it's read from the RigidbodyInteraction component.")]
        [SerializeField] private bool isKinematicVelocity;

        [Tooltip("Determines if the parent object's velocity should be considered instead of the colliding object.")]
        [SerializeField] private bool isParentVelocity;

        [Header("Name Filter")]
        [Tooltip("Filters collision detection based on the name of the collider object.")]
        [SerializeField] private string colliderNameFilter;

        [Tooltip("Returns true if the collider name contains the string input above, does not need to be a exact match")]
        [SerializeField] private bool nameContains;

        [Tooltip("Checks the parent's name of the collider object instead of the collider itself.")]
        [SerializeField] private bool isParentName;

        [Header("Events")]
        [Tooltip("Event triggered when OnCollisionEnter conditions are met.")]
        [SerializeField] private UltEvent OnCollisionEnterCheckMetEvent;

        [Tooltip("Event triggered when OnCollisionExit conditions are met.")]
        [SerializeField] private UltEvent OnCollisionExitCheckMetEvent;

        [Tooltip("Event triggered when OnCollisionStay conditions are met.")]
        [SerializeField] private UltEvent OnCollisionStayCheckMetEvent;

        private Rigidbody ownRigidbody;
        private int currentNumberOfEnterChecks;
        private int currentNumberOfExitChecks;
        private int currentNumberOfStayChecks;
        private bool conditionMetEnter;
        private bool conditionMetExit;
        private bool conditionMetStay;
        private HashSet<GameObject> alreadyCheckedCollidersEnter = new HashSet<GameObject>();
        private HashSet<GameObject> alreadyCheckedCollidersExit = new HashSet<GameObject>();
        private HashSet<GameObject> alreadyCheckedCollidersStay = new HashSet<GameObject>();

        /// <summary>
        /// Initializes the component, setting up references.
        /// </summary>
        private void Start()
        {
            ownRigidbody = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckCollision(collision.collider.gameObject, CheckType.OnEnter);
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckCollision(other.gameObject, CheckType.OnEnter);
        }

        private void OnCollisionExit(Collision collision)
        {
            CheckCollision(collision.collider.gameObject, CheckType.OnExit);
        }

        private void OnTriggerExit(Collider other)
        {
            CheckCollision(other.gameObject, CheckType.OnExit);
        }

        private void OnCollisionStay(Collision collision)
        {
            CheckCollision(collision.collider.gameObject, CheckType.OnStay);
        }

        private void OnTriggerStay(Collider other)
        {
            CheckCollision(other.gameObject.gameObject, CheckType.OnStay);
        }

        private void OnParticleCollision(GameObject other)
        {
            CheckCollision(other, CheckType.OnEnter);
        }

        private void OnParticleTrigger()
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            Component component = ps.trigger.GetCollider(0);
            CheckCollision(component.gameObject, CheckType.OnEnter);
        }

        /// <summary>
        /// Checks the collision against specified filters and updates counters.
        /// </summary>
        private void CheckCollision(GameObject other, CheckType checkType)
        {
            if (!IsNameFilterPassed(other) || !IsVelocityCheckPassed(other)) return;

            HashSet<GameObject> colliderList;
            switch (checkType)
            {
                case CheckType.OnEnter:
                    colliderList = alreadyCheckedCollidersEnter;
                    break;
                case CheckType.OnExit:
                    colliderList = alreadyCheckedCollidersExit;
                    break;
                case CheckType.OnStay:
                    colliderList = alreadyCheckedCollidersStay;
                    break;
                default:
                    return; // Unknown CheckType
            }

            if (!allowSameObjectRecollision && colliderList.Contains(other)) return;

            HandleCollisionActions(other);
            UpdateCollisionCount(other, checkType);
        }


        /// <summary>
        /// Checks if the collision passes the name filter.
        /// </summary>
        private bool IsNameFilterPassed(GameObject colliderObject)
        {
            string targetName = isParentName ? colliderObject.transform.parent.name : colliderObject.name;

            if (nameContains)
                return targetName.Contains(colliderNameFilter);
            else
                return targetName == colliderNameFilter;
        }

        /// <summary>
        /// Checks if the collision passes the velocity filter.
        /// </summary>
        private bool IsVelocityCheckPassed(GameObject colliderObject)
        {
            if (requiredVelocityVector == Vector3.zero) return true;

            GameObject rigidbodyHolder = isParentVelocity ? colliderObject.transform.parent.gameObject : colliderObject;

            Vector3 velocity = isKinematicVelocity ? rigidbodyHolder.GetComponent<RigidbodyInteraction>().GetKinematicVelocity() : rigidbodyHolder.GetComponent<Rigidbody>().velocity;
            velocity = rigidbodyHolder.GetComponent<RigidbodyInteraction>().GetKinematicVelocity();

            return rigidbodyHolder != null && Vector3.Dot(velocity, requiredVelocityVector.normalized) > 0; ;
        }

        /// <summary>
        /// Handles actions to be taken upon collision, like deactivating or triggering feedback.
        /// </summary>
        private void HandleCollisionActions(GameObject collider)
        {
            // Deactivate Collided Interactable
            var interactable = collider.GetComponent<Interactable>();
            if (interactable && deactivateCollidedInteractables)
            {
                interactable.DeactivateInteractable();
            }

            // Trigger Action Feedback
            var actionFeedback = collider.GetComponent<ActionFeedback>();
            if (actionFeedback && triggerActionFeedback)
            {
                actionFeedback.PlayRandomTriggerFeedback();
            }
        }

        /// <summary>
        /// Updates the collision count and checks if conditions are met to trigger an event.
        /// </summary>
        private void UpdateCollisionCount(GameObject collider, CheckType checkType)
        {
            int currentCount;
            bool conditionMet;
            HashSet<GameObject> colliderList;

            switch (checkType)
            {
                case CheckType.OnEnter:
                    currentCount = ++currentNumberOfEnterChecks;
                    conditionMet = conditionMetEnter;
                    colliderList = alreadyCheckedCollidersEnter;
                    break;
                case CheckType.OnExit:
                    currentCount = ++currentNumberOfExitChecks;
                    conditionMet = conditionMetExit;
                    colliderList = alreadyCheckedCollidersExit;
                    break;
                case CheckType.OnStay:
                    currentCount = ++currentNumberOfStayChecks;
                    conditionMet = conditionMetStay;
                    colliderList = alreadyCheckedCollidersStay;
                    break;
                default:
                    return;
            }

            if (!allowSameObjectRecollision)
            {
                colliderList.Add(collider);
            }

            if (currentCount >= numberOfChecksToTrigger && !conditionMet)
            {
                OnCollisionCheckMet(checkType);
                colliderList.Clear();
            }

            if (currentCount >= numberOfChecksToTrigger && !resetCheckOnceMet)
            {
                switch (checkType)
                {
                    case CheckType.OnEnter:
                        conditionMetEnter = true;
                        break;
                    case CheckType.OnExit:
                        conditionMetExit = true;
                        break;
                    case CheckType.OnStay:
                        conditionMetStay = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Invokes the appropriate event when the collision conditions are met.
        /// </summary>
        /// <param name="checkType">The type of check (OnEnter or OnExit) that met the conditions.</param>
        private void OnCollisionCheckMet(CheckType checkType)
        {
            switch (checkType)
            {
                case CheckType.OnEnter:
                    OnCollisionEnterCheckMetEvent?.Invoke();
                    ResetCollisionCounters(CheckType.OnEnter);
                    break;
                case CheckType.OnExit:
                    OnCollisionExitCheckMetEvent?.Invoke();
                    ResetCollisionCounters(CheckType.OnExit);
                    break;
                default:
                    OnCollisionStayCheckMetEvent?.Invoke();
                    ResetCollisionCounters(CheckType.OnStay);
                    break;
            }
        }

        /// <summary>
        /// Resets the collision counters and conditions based on the check type.
        /// </summary>
        /// <param name="checkType">The type of check (OnEnter or OnExit) for which to reset counters.</param>
        private void ResetCollisionCounters(CheckType checkType)
        {
            if (resetCheckOnceMet)
            {
                switch (checkType)
                {
                    case CheckType.OnEnter:
                        currentNumberOfEnterChecks = 0;
                        conditionMetEnter = false;
                        break;
                    case CheckType.OnExit:
                        currentNumberOfExitChecks = 0;
                        conditionMetExit = false;
                        break;
                    case CheckType.OnStay:
                        currentNumberOfStayChecks = 0;
                        conditionMetStay = false;
                        break;
                }
            }
        }
    }
}