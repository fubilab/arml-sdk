using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the placement of objects onto a target, with support for auto-placement, locking, and name filtering.
/// </summary>
public class PlacementTarget : Interactable
{
    [field: Header("Object Properties")]
    [field: SerializeField]
    public Transform target { get; private set; }
    public GameObject displayMesh;

    [field: Header("Placement settings")]
    [field: SerializeField]
    public bool LockObjectOnPlacement { get; protected set; } = false;
    [field: SerializeField, Tooltip("Whether the target Grabbable should auto-place itself when the game first runs")]
    public bool StartWithObjectPlaced { get; protected set; } = false;
    [field: SerializeField]
    public Grabbable ObjectToAutoPlace { get; protected set; }
    public bool HasObject { get { return placedObject != null; } }

    public GameObject pendingPlacedObject { get; protected set; }
    public GameObject placedObject { get; protected set; }
    public GameObject lastPlacedObject { get; protected set; }
    public bool canPlaceLastPlacedObject { get; protected set; } = true; //used to prevent perpetual place and grab
    [Tooltip("Minimum separation distance between the object that was picked and the placeable target that needs to happen before being able to place it again (this avoid instants placement upon pickup)")]
    public float minimumSquaredDistanceToPlaceAgain = 0.2f;
    [field: Header("Name filtering")]
    [field: SerializeField]
    public bool NameFilter { get; private set; } = false;
    [field: SerializeField]
    protected string[] Names { get; private set; }

    [Header("Events")]
    [SerializeField] private UnityEvent OnObjectPlacedEvent;
    [SerializeField] private UnityEvent OnObjectRemovedEvent;

    public static Action OnStartPlacingAttemptAction;
    public static Action OnStopPlacingAttemptAction;

    /// <summary>
    /// Initializes the placement target and subscribes to interaction timer events.
    /// </summary>
    private void Awake()
    {
        base.Awake();
        iTimer.OnFinishInteraction += this.Place;
    }

    /// <summary>
    /// Starts with an object placed on the target if configured.
    /// </summary>
    private void Start()
    {
        base.Start();

        //Auto-place target object
        if (StartWithObjectPlaced && ObjectToAutoPlace != null)
        {
            AutoPlaceObject();
        }
    }

    private void AutoPlaceObject()
    {
        //Grabbable
        ObjectToAutoPlace.pendingPlaceable = this;
        ObjectToAutoPlace.Place();

        //Placeable
        pendingPlacedObject = ObjectToAutoPlace.gameObject;
        Place();
    }

    /// <summary>
    /// Attaches the placed object to the target each frame.
    /// </summary>
    private void FixedUpdate()
    {
        if (placedObject != null)
        { //attach the object manually each frame.
            placedObject.transform.position = this.target.position;
            placedObject.transform.rotation = this.target.rotation;
        }
        if (!canPlaceLastPlacedObject)
        {
            if (lastPlacedObject == null)
                canPlaceLastPlacedObject = true;
            else
                canPlaceLastPlacedObject = (target.transform.position - lastPlacedObject.transform.position).sqrMagnitude > minimumSquaredDistanceToPlaceAgain;
        }
    }

    /// <summary>
    /// Checks if the given object passes the name filter.
    /// </summary>
    bool CheckNamesFilter(GameObject other)
    {
        if (!NameFilter)
            return true;

        for (int i = 0; i < Names.Length; i++)
        {
            if (other.gameObject.name == Names[i])
                return true;
        }

        return false;
    }

    /// <summary>
    /// Initiates the process of placing an object onto the target.
    /// </summary>
    public void StartPlacingObject(GameObject other)
    {
        pendingPlacedObject = other;

        if (interactionType == InteractionType.DWELL)
            iTimer.StartInteraction();

        AddOutlineMaterial(displayMesh.GetComponent<Renderer>());

        OnStartPlacingAttemptAction?.Invoke();
    }

    /// <summary>
    /// Stops the process of placing an object onto the target.
    /// </summary>
    public void StopPlacingObject()
    {
        pendingPlacedObject = null;
        iTimer.CancelInteraction();
        RemoveOutlineMaterial(displayMesh.GetComponent<Renderer>());

        OnStopPlacingAttemptAction?.Invoke();
    }

    /// <summary>
    /// Places the pending object onto the target.
    /// </summary>
    public void Place()
    {
        placedObject = pendingPlacedObject;
        pendingPlacedObject = null;
        RemoveOutlineMaterial(displayMesh.GetComponent<Renderer>());
        displayMesh.SetActive(false);
        actionFeedback?.PlayRandomTriggerFeedback();
        OnObjectPlacedEvent?.Invoke();
    }

    /// <summary>
    /// Removes the currently placed object from the target.
    /// </summary>
    public void Remove()
    {
        lastPlacedObject = placedObject;
        placedObject = null;
        displayMesh.SetActive(true);
        canPlaceLastPlacedObject = false;
        OnObjectRemovedEvent?.Invoke();
        RemoveOutlineMaterial(displayMesh.GetComponent<Renderer>());
    }

    /// <summary>
    /// Checks if a specific object can be placed on the target based on current conditions.
    /// </summary>
    /// <param name="other">The GameObject to check for placement.</param>
    /// <returns>True if the object can be placed, false otherwise.</returns>
    public bool CheckIfCanPlaceObject(GameObject other)
    {
        bool canPlace = pendingPlacedObject == null && placedObject == null;
        bool isLastObject = other == lastPlacedObject;
        return (canPlace && (!isLastObject || (isLastObject && canPlaceLastPlacedObject)) && CheckNamesFilter(other));
    }

    /// <summary>
    /// Wrapper to update the outline material from the Grabbable's OnTriggerStay
    /// </summary>
    public void UpdatePlacing()
    {
        UpdateOutlineFill(displayMesh.GetComponent<Renderer>());
    }

    //Not currently used
    private void OnTriggerStay(Collider other)
    {

    }

    /// <summary>
    /// Handles user input for placement actions when the interaction type is set to BUTTON.
    /// </summary>
    private void Update()
    {
        if (pendingPlacedObject == null || interactionType != InteractionType.BUTTON)
            return;

        //Button Place
        if (Input.GetMouseButtonDown(0))
        {
            iTimer.OnFinishInteraction();
        }
    }

    protected override void CheckVoiceCommand(string _command)
    {
        //Check all voice commands regardless of their VoiceCommandAction enum
        foreach (var command in voiceCommandKeywords)
        {
            if (_command.Contains(command.keyword, StringComparison.InvariantCultureIgnoreCase))
            {
                AutoPlaceObject();
                break;
            }
        }
    }
}
