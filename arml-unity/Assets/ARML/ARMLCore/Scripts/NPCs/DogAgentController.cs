using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace ARML
{
    /// <summary>
    /// Controls a dog agent using NavMesh navigation, animation, and interaction behaviors.
    /// </summary>
    public class DogAgentController : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform; // The target transform for the agent.
        [SerializeField] private Transform originTransform;  // The origin transform for the agent.
        [SerializeField] private bool originIsMainCamera;
        [SerializeField, Range(0, 1)] private float distancePercentage = 0.5f; // The percentage of the distance between target and origin.
        [SerializeField] private float minimumDistanceToTarget = 2f; // Minimum distance to maintain from the target transform.
        [SerializeField] private float agentSpeed = 1f; // Minimum distance to maintain from the target transform.

        [SerializeField] LayerMask layerMask;

        private NavMeshAgent agent;  // The NavMeshAgent component.
        private Animator anim;      // The Animator component for animations.
        private ActionFeedback actionFeedback;
        private Vector2 smoothDeltaPosition = Vector2.zero;
        private Vector2 velocity = Vector2.zero;
        private float initialDistanceToTarget; // Initial distance to the target.
        private const float ThresholdDistance = 1f; // Distance at which distancePercentage becomes 1.

        private bool isWalkingBack;
        private Camera mainCam;

        private bool increasedPercentage;
        private bool currentlyMovingToDestination;
        private bool currentlyTurningTowardsTarget;
        private Vector3 previousDestination;

        private RaycastHit hit;

        private Coroutine turnTowardsTargetCo;

        private Vector3 newDestination;

        public float distanceMultiplier;

        public float distanceBetweenPointsThreshold;

        public float distanceToStartBarking;

        /// <summary>
        /// Called before the first frame update, initializes the agent, animator, and action feedback components.
        /// </summary>
        void Start()
        {
            agent = GetComponent<NavMeshAgent>(); // Get the NavMeshAgent component.
            anim = GetComponent<Animator>();      // Get the Animator component.
            actionFeedback = GetComponent<ActionFeedback>();
            mainCam = Camera.main;

            // Don't update position automatically to handle it manually.
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        /// <summary>
        /// Update is called once per frame to calculate speed and animation based on agent movement.
        /// </summary>
        void LateUpdate()
        {
            if (!agent) return;

            if (mainCam == null)
            {
                mainCam = Camera.main;
                return;
            }

            if (originIsMainCamera)
                originTransform = mainCam.transform;

            CalculateSpeedAnimation();
            FaceTarget();
            SetNewDestinationCamRaycast();

            CheckDistanceToTarget();
        }

        void SetNewDestinationCamRaycast()
        {
            //If more than given X axis (looking down) but less than 90 (to prevent looking up angles)
            if (mainCam.transform.eulerAngles.x > 15 && mainCam.transform.eulerAngles.x < 90)
            {
                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 3f, layerMask))
                {
                    if (currentlyMovingToDestination)
                    {
                        //If reached destination
                        if (agent.remainingDistance <= agent.stoppingDistance)
                        {
                            //Reached destination
                            currentlyMovingToDestination = false;
                            Debug.Log("reached destination");

                            //Start turning to target coroutine
                            //if (!currentlyTurningTowardsTarget && turnTowardsTargetCo == null)
                            if (!currentlyTurningTowardsTarget && turnTowardsTargetCo == null)
                                turnTowardsTargetCo = StartCoroutine(TurnTowardsTarget());

                            return;
                        }
                    }

                    if (currentlyTurningTowardsTarget) return;

                    float distanceBetweenPoints = Vector3.Distance(hit.point, newDestination);

                    //Set new destination if it's far away enough from the previous one
                    if (distanceBetweenPoints > distanceBetweenPointsThreshold + agent.stoppingDistance)
                    {
                        newDestination = hit.point;
                        agent.SetDestination(newDestination);
                        //Interrupt turning to target coroutine
                        if (turnTowardsTargetCo != null)
                        {
                            StopCoroutine(turnTowardsTargetCo);
                            turnTowardsTargetCo = null;
                        }

                        // Check if the path is valid
                        StartCoroutine(CheckPathStatus());

                        currentlyTurningTowardsTarget = false;
                        currentlyMovingToDestination = true;
                    }
                }
            }
            else
            {
                currentlyMovingToDestination = false;
            }
        }

        private IEnumerator CheckPathStatus()
        {
            yield return new WaitForEndOfFrame(); // Wait for the next frame to ensure the path status is updated

            if (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                agent.ResetPath(); // Stop the agent
                currentlyMovingToDestination = false;
                Debug.Log("Cannot reach the destination");
            }
        }

        private void CheckDistanceToTarget()
        {
            float currentDistanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
            Debug.Log(currentDistanceToTarget);

            if (distanceToStartBarking > currentDistanceToTarget)
            {
                if (!IsInvoking(nameof(Bark)))
                    InvokeRepeating(nameof(Bark), 0, 2f);
            }
            else
            {
                StopBarking();
            }
        }

        /// <summary>
        /// (Currently unused) Makes the agent move to a clicked point on the navmesh surface.
        /// </summary>
        void GoToClick()
        {
            if (Camera.main == null) return;

            if (Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (agent)
                        agent.SetDestination(hit.point);
                }
            }
        }

        /// <summary>
        /// Calculates the speed and animations based on the agent's movement.
        /// </summary>
        void CalculateSpeedAnimation()
        {
            Vector3 worldDeltaPosition = agent.nextPosition - transform.position;

            // Map 'worldDeltaPosition' to local space
            float dx = Vector3.Dot(transform.right, worldDeltaPosition);
            float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
            Vector2 deltaPosition = new Vector2(dx, dy);

            // Low-pass filter the deltaMove
            float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
            smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

            // Update velocity if time advances
            if (Time.deltaTime > 1e-5f)
                velocity = smoothDeltaPosition / Time.deltaTime;

            // Determine if the agent should move and update the animation accordingly.
            bool shouldMove = velocity.magnitude > 0.002f && agent.remainingDistance >= agent.stoppingDistance;

            // Update animation parameters
            anim.SetBool("moving", shouldMove);

            // Pull character towards agent if necessary
            if (worldDeltaPosition.magnitude > agent.radius)
                transform.position = agent.nextPosition - 0.9f * worldDeltaPosition;
        }

        /// <summary>
        /// Update the position based on animation movement using navigation surface height.
        /// </summary>
        void OnAnimatorMove()
        {
            //Update transform position based on agent
            Vector3 position = anim.rootPosition;
            position.y = agent.nextPosition.y;
            transform.position = position;
        }

        /// <summary>
        /// Adjusts the dog's facing direction based on the target and navigation path.
        /// </summary>
        void FaceTarget()
        {
            //Calculate original steering turn
            var turnTowardNavSteeringTarget = agent.steeringTarget;
            Vector3 turnDirection = (turnTowardNavSteeringTarget - transform.position).normalized;
            Quaternion turnLookRotation = Quaternion.LookRotation(new Vector3(turnDirection.x, 0, turnDirection.z));

            //Calculate look to point towards target
            // Get the direction from the source to the target.
            Vector3 directionToTarget = targetTransform.position - transform.position;
            // Calculate the rotation as an Euler angle in degrees.
            float angle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;

            //If difference between steer turn and lookAtTarget directions is small, allow steer turn
            if (Mathf.Abs(angle - turnLookRotation.eulerAngles.y) < 120)
            {
                //transform.rotation = Quaternion.Slerp(transform.rotation, turnLookRotation, Time.deltaTime * 2); //Manual technique
                agent.speed = agentSpeed;
                anim.SetFloat("walkSpeed", agentSpeed);
                if (isWalkingBack == false) return; //If already turning, return, so only happens once
                StopBarking();
                agent.updateRotation = true;
                isWalkingBack = false;
            }
            else
            {
                isWalkingBack = true;
                agent.updateRotation = true;
            }
        }

        //[Button]
        public void IncreaseDistancePercentage()
        {
            if (!increasedPercentage)
            {
                distancePercentage = distancePercentage + 0.15f;
                increasedPercentage = true;
            }
        }

        public void ParticipantNotLooking()
        {
            Bark();

            if (increasedPercentage)
            {
                distancePercentage = distancePercentage - 0.2f;
                increasedPercentage = false;
            }
        }

        public IEnumerator TurnTowardsTarget()
        {
            yield return new WaitForSeconds(0.5f);

            SetNewDestination();

            currentlyTurningTowardsTarget = true;

            while (agent.remainingDistance > agent.stoppingDistance)
            {
                yield return null;
            }

            currentlyTurningTowardsTarget = false;

            turnTowardsTargetCo = null;
        }

        private void SetNewDestination()
        {
            Vector3 directionToTarget = targetTransform.position - transform.position;

            // Normalize the direction vector
            Vector3 normalizedDirection = directionToTarget.normalized;

            // Scale the direction by the step size
            Vector3 stepVector = normalizedDirection * distanceMultiplier;

            // Calculate the new destination
            Vector3 newDestination = transform.position + stepVector;

            // Ensure the new destination does not overshoot the target
            if (Vector3.Distance(transform.position, targetTransform.position) < distanceMultiplier)
            {
                newDestination = targetTransform.position;
            }

            agent.SetDestination(newDestination);
            Debug.Log("New Destination: " + newDestination);
        }

        /// <summary>
        /// Defines the bark behavior which is triggered under specific conditions.
        /// </summary>
        public void Bark()
        {
            anim.SetTrigger("bark");
            actionFeedback.PlayRandomTriggerFeedback();
        }

        public void StopBarking()
        {
            if (IsInvoking(nameof(Bark)))
                CancelInvoke(nameof(Bark));
        }

        private void UpdateDistancePercentage()
        {
            float currentDistanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
            float distanceFactor = Mathf.Clamp01((initialDistanceToTarget - currentDistanceToTarget) / (initialDistanceToTarget - ThresholdDistance));
            distancePercentage = 0.3f + (0.3f - distanceFactor * 0.3f);
        }

        public void SetTransformTarget(Transform target)
        {
            targetTransform = target;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                if (hit.point != null)
                    Gizmos.DrawWireSphere(hit.point, 0.2f);
            }

        }
    }
}