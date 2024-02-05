using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

public enum GrabbableState { FREE, GRABBED, PLACED, LOCKED }

/// <summary>
/// An abstract class representing an object that can be interacted with in various ways, such as grabbing and placing.
/// </summary>
public abstract class Grabbable : Interactable
{
    [field: Header("Physics")]
    public Rigidbody rb { get; protected set; }
    protected bool usesGravity;

    // ---- States
    [HideInInspector] public GrabbableState state { get; protected set; } = GrabbableState.FREE;
    [HideInInspector] public GrabbableState previousState { get; protected set; } = GrabbableState.FREE;

    [field: Header("Grab")]
    public PlacementTarget placeable { get; protected set; }
    public PlacementTarget pendingPlaceable { get; set; }
    [field: SerializeField]

    protected Transform grabTarget; //Target that the grabbable will follow when grabbed.

    protected bool attemptingToGrab { get; private set; } = false;

    [field: SerializeField]
    protected float lerpScale { get; private set; } = 7.0f;

    [Header("Collider Scaling")]
    [SerializeField, Tooltip("Multiplies the collider by this amount when grabbed, useful for interacting with things at a distance or to avoid having" +
        "to move to a very specific point to place it etc.")] 
    private Vector3 grabColliderScaleMultiplier = new Vector3(1f, 1f, 1f);
    private Vector3 originalColliderSize;

    protected BoxCollider grabbableCollider;

    [field: Header("Event")]
    [SerializeField] protected UnityEvent OnObjectGrabbedEvent;
    [field: Tooltip("This event is called when the button is pressed WHILE the Grabbable is currently being grabbed")]
    [SerializeField] protected UnityEvent OnButtonDownWhileGrabbedEvent;
    [SerializeField] protected UnityEvent OnButtonUpWhileGrabbedEvent;

    // ---- Grab subscribable void actions
    public static Action OnStartGrabbingAttemptAction;
    public static Action OnStopGrabbingAttemptAction;
    public static Action<Grabbable> OnGrabSuccesfulAction; //Gives reference to currently grabbed object
    public Action OnPlace; //This needs to be instanced for CameraGrabber
    public static Action OnReleaseAction;

    protected Camera cam;

    /// <summary>
    /// Initializes the grabbable object, setting up necessary components and states.
    /// </summary>
    protected virtual void Awake()
    {
        base.Awake();
        rb = model.GetComponent<Rigidbody>();
        usesGravity = rb.useGravity;
        cam = Camera.main;

        grabbableCollider = GetComponent<BoxCollider>();
        if (grabbableCollider != null)
        {
            // Store the original collider size
            originalColliderSize = grabbableCollider.size;
        }
    }

    /// <summary>
    /// Initiates an attempt to grab the object, updating its state and outline material.
    /// </summary>
    public virtual bool StartGrabbingAttempt(Transform target)
    {
        if (attemptingToGrab)
            return false;

        if (state != GrabbableState.FREE && state != GrabbableState.PLACED)
            return false;

        attemptingToGrab = true;

        if (interactionType == InteractionType.DWELL)
            iTimer.StartInteraction();

        //Set outline material
        if (outlineMaterial != null)
            AddOutlineMaterial(renderer);

        this.grabTarget = target;
        if (OnStartGrabbingAttemptAction != null)
            OnStartGrabbingAttemptAction();
        return true;
    }

    /// <summary>
    /// Handles the logic when the object is successfully grabbed.
    /// </summary>
    public virtual void Grab()
    {
        if (state == GrabbableState.PLACED || state == GrabbableState.LOCKED)
        { //Grab can be forced that why we consider the LOCKED state
            RemoveFromPlace();
        }
        attemptingToGrab = false;
        UpdateState(state, GrabbableState.GRABBED);
        this.gameObject.layer = LayerMask.NameToLayer("Grabbed"); // Not efficient but solid when changing projects
        if (OnGrabSuccesfulAction != null)
            OnGrabSuccesfulAction(this);

        RemoveOutlineMaterial(renderer);

        // Scale the collider when grabbed
        if (grabbableCollider != null)
        {
            grabbableCollider.size = originalColliderSize.Multiply(grabColliderScaleMultiplier);
        }
    }

    /// <summary>
    /// Stops the current grabbing attempt and releases the object.
    /// </summary>
    public virtual void StopGrabbingAttempt()
    {
        iTimer.CancelInteraction();
        grabTarget = null;
        ReleaseFromGrab();
        if (OnStopGrabbingAttemptAction != null)
            OnStopGrabbingAttemptAction();
    }

    /// <summary>
    /// Releases the object, resetting its state and outline material.
    /// </summary>
    public virtual void Release()
    {
        ReleaseFromGrab();
        UpdateState(state, GrabbableState.FREE);
        if (OnReleaseAction != null)
            OnReleaseAction();
    }

    protected virtual void ReleaseFromGrab()
    {
        grabTarget = null;
        attemptingToGrab = false;
        this.gameObject.layer = LayerMask.NameToLayer("Grabbable");

        RemoveOutlineMaterial(renderer);
    }

    /// <summary>
    /// Abstract method to update the position of the grabbed object.
    /// </summary>
    public abstract void UpdateGrabbedPosition();

    /// <summary>
    /// Places the object at a specified target.
    /// </summary>
    public void Place()
    {
        placeable = pendingPlaceable;
        pendingPlaceable = null;
        grabTarget = null;

        UpdateState(state, placeable.LockObjectOnPlacement ? GrabbableState.LOCKED : GrabbableState.PLACED);
        this.gameObject.layer = LayerMask.NameToLayer("Grabbable");

        rb.isKinematic = true;
        rb.MovePosition(placeable.target.transform.position);
        rb.MoveRotation(placeable.target.transform.rotation);

        placeable.iTimer.OnFinishInteraction -= this.Place;
        if (OnPlace != null)
            OnPlace();

        RemoveOutlineMaterial(renderer);

        // Reset the collider size when placed
        if (grabbableCollider != null)
        {
            grabbableCollider.size = originalColliderSize;
        }
    }

    /// <summary>
    /// Removes the object from its placement.
    /// </summary>
    public void RemoveFromPlace()
    {
        rb.isKinematic = false;
        Debug.Log("Removing from placeable");
        placeable.Remove();
        placeable = null;
        RemoveOutlineMaterial(renderer);
    }

    /// <summary>
    /// Updates the state of the grabbable object.
    /// </summary>
    protected void UpdateState(GrabbableState prevState, GrabbableState targetState)
    {
        previousState = prevState;
        state = targetState;
    }

    /// <summary>
    /// Calculates the vector from the camera to the grab target.
    /// </summary>
    protected Vector3 CalculateCameraToTargetVector()
    {
        return (grabTarget.transform.position - Camera.main.transform.position).normalized;
    }

    /// <summary>
    /// Handles logic when entering a trigger zone, potentially initiating a placement.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        //Do not check if already placed
        if (state == GrabbableState.PLACED || state == GrabbableState.LOCKED)
            return;

        PlacementTarget p = other.GetComponent<PlacementTarget>();
        if (p != null)
        {
            if (p.CheckIfCanPlaceObject(this.gameObject))
            {
                pendingPlaceable = p;
                p.iTimer.OnFinishInteraction += this.Place;
                p.StartPlacingObject(this.gameObject);
            }
        }
    }

    /// <summary>
    /// Handles logic when exiting a trigger zone, potentially stopping a placement.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        PlacementTarget p = other.GetComponent<PlacementTarget>();
        if (p != null && p == pendingPlaceable)
        {
            p.iTimer.OnFinishInteraction -= this.Place;
            p.StopPlacingObject();
        }
    }

    /// <summary>
    /// Continuously updates the placement status of the object while within a trigger zone. 
    /// Used to update the fill rate of the placeable outline material
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        //Placeable
        PlacementTarget p = other.GetComponent<PlacementTarget>();
        if (p != null)
        {
            p.UpdatePlacing();
        }
    }

    /// <summary>
    /// Updates the grabbable object during the grabbing process, such as updating the outline fill.
    /// </summary>
    public void GrabbingUpdate()
    {
        if (state == GrabbableState.GRABBED || state == GrabbableState.LOCKED)
            return;

        //Set outline material
        if (outlineMaterial != null)
            UpdateOutlineFill(renderer);
    }

    /// <summary>
    /// Handles user input for additional actions while the object is grabbed.
    /// </summary>
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //Button Grab
            if (attemptingToGrab && interactionType == InteractionType.BUTTON)
            {
                iTimer.OnFinishInteraction();
                return; //This prevents invoking the ButtonDown event immediately after
            }

            if (state == GrabbableState.GRABBED)
            {
                OnButtonDownWhileGrabbedEvent?.Invoke();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (state == GrabbableState.GRABBED)
            {
                OnButtonUpWhileGrabbedEvent?.Invoke();
            }
        }
    }
}
