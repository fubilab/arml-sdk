using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls a dog agent using NavMesh navigation, animation, and interaction behaviors.
/// </summary>
public class DogAgentController : MonoBehaviour
{
    [SerializeField] private Transform targetTransform; // The target transform for the agent.
    [SerializeField] private Transform crateTransform;  // The crate transform for intermediate destination.
    [SerializeField, Range(0, 1)] private float distancePercentage = 0.5f; // The percentage of the distance between target and crate.

    private NavMeshAgent agent;  // The NavMeshAgent component.
    private Animator anim;      // The Animator component for animations.
    private ActionFeedback actionFeedback;
    private Vector2 smoothDeltaPosition = Vector2.zero;
    private Vector2 velocity = Vector2.zero;
    private float initialDistanceToTarget; // Initial distance to the target.
    private const float ThresholdDistance = 1f; // Distance at which distancePercentage becomes 1.

    /// <summary>
    /// Called before the first frame update, initializes the agent, animator, and action feedback components.
    /// </summary>
    void Start()
    {
        agent = GetComponent<NavMeshAgent>(); // Get the NavMeshAgent component.
        anim = GetComponent<Animator>();      // Get the Animator component.
        actionFeedback = GetComponent<ActionFeedback>();

        // Don't update position automatically to handle it manually.
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    /// <summary>
    /// Update is called once per frame to calculate speed and animation based on agent movement.
    /// </summary>
    void Update()
    {
        // Calculate speed and animations based on agent's movement.
        CalculateSpeedAnimation();

        if (!agent) return;

        // Update the distancePercentage based on the current distance to the target.
        UpdateDistancePercentage();

        // Place the agent in between the target and the crate using distancePercentage.
        agent.SetDestination(targetTransform.position + (crateTransform.position - targetTransform.position) * (1 - distancePercentage));

        FaceTarget();
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
        bool shouldMove = velocity.magnitude > 0.005f && agent.remainingDistance >= agent.stoppingDistance;

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
            agent.speed = 1f;
            anim.SetFloat("walkSpeed", 1f);
            if (agent.updateRotation == true) return; //If already turning, return, so only happens once
            CancelInvoke(nameof(Bark)); //Using string version as it stops all the bark coroutines
            agent.updateRotation = true;
        }
        else
        {
            agent.speed = 0f;
            anim.SetBool("moving", false);
            //anim.SetFloat("walkSpeed", 0.75f);
            if (agent.updateRotation == false) return; //If already stopped, return, so only happens once
            InvokeRepeating(nameof(Bark), 1f, 2f); //Bark cause you are going in different direction
            agent.updateRotation = false;
        }
    }

    /// <summary>
    /// Defines the bark behavior which is triggered under specific conditions.
    /// </summary>
    private void Bark()
    {
        anim.SetTrigger("bark");
        actionFeedback.PlayRandomTriggerFeedback();
    }

    private void UpdateDistancePercentage()
    {
        float currentDistanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
        float distanceFactor = Mathf.Clamp01((initialDistanceToTarget - currentDistanceToTarget) / (initialDistanceToTarget - ThresholdDistance));
        distancePercentage = 0.3f + (0.3f - distanceFactor * 0.3f);
    }
}
