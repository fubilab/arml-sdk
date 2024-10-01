using System;
using UnityEngine;
using UnityEngine.Events;

namespace ARML
{
    /// <summary>
    /// Manages the placement of objects onto a target, with support for auto-placement, locking, and name filtering.
    /// </summary>
    public class PlacementTarget : Interactable
    {
        [field: Header("Object Properties")]
        [field: SerializeField, Tooltip("The transform where objects will be placed.")]
        public Transform target { get; private set; }

        [Tooltip("The mesh that will be displayed on the placement target.")]
        public GameObject displayMesh;

        [field: Header("Placement settings")]
        [field: SerializeField, Tooltip("Whether to lock the object on placement, preventing further interaction.")]
        public bool LockObjectOnPlacement { get; protected set; } = false;

        [field: SerializeField, Tooltip("If true, the target Grabbable will auto-place itself when the game starts.")]
        public bool StartWithObjectPlaced { get; protected set; } = false;

        [field: SerializeField, Tooltip("The Grabbable object that will be auto-placed at the start.")]
        public Grabbable ObjectToAutoPlace { get; protected set; }

        /// <summary>
        /// Indicates whether there is currently an object placed on this target.
        /// </summary>
        public bool HasObject => placedObject != null;

        [Tooltip("If true, smooth placement will be used when placing objects.")]
        public bool SmoothPlacement = true;

        [Tooltip("The speed at which objects will smoothly transition to the target position.")]
        public float SmoothPlacementSpeed = 2f;

        /// <summary>
        /// The collider that detects interactions with this target.
        /// </summary>
        public Collider grabCollider;

        /// <summary>
        /// The object that is currently pending placement.
        /// </summary>
        public GameObject pendingPlacedObject { get; protected set; }

        /// <summary>
        /// The object currently placed on the target.
        /// </summary>
        public GameObject placedObject { get; protected set; }

        /// <summary>
        /// The last object that was placed on the target.
        /// </summary>
        public GameObject lastPlacedObject { get; protected set; }

        /// <summary>
        /// Indicates if the last placed object can be placed again.
        /// </summary>
        public bool canPlaceLastPlacedObject { get; protected set; } = true;

        [Tooltip("Minimum separation distance required before being able to place an object again. Prevents instant placement upon pickup.")]
        public float minimumSquaredDistanceToPlaceAgain = 0.2f;

        [field: Header("Name filtering")]
        [field: SerializeField, Tooltip("If true, only objects with names that match the specified list can be placed.")]
        public bool NameFilter { get; private set; } = false;

        [field: SerializeField, Tooltip("Array of names that are allowed to be placed on this target.")]
        protected string[] Names { get; private set; }

        [Tooltip("If true, prevents placing after the first pickup.")]
        public bool preventPlacingAfterFirstPickUp;

        [Header("Events")]
        [SerializeField, Tooltip("Event triggered when an object is successfully placed on the target.")]
        private UnityEvent OnObjectPlacedEvent;

        [SerializeField, Tooltip("Event triggered when an object is removed from the target.")]
        private UnityEvent OnObjectRemovedEvent;

        public static Action OnStartPlacingAttemptAction;
        public static Action OnStopPlacingAttemptAction;

        /// <summary>
        /// Initializes the placement target and subscribes to interaction timer events.
        /// </summary>
        private void Awake()
        {
            base.Awake();
            iTimer.OnFinishInteraction += this.Place;
            grabCollider = GetComponent<Collider>();
        }

        /// <summary>
        /// Starts with an object placed on the target if configured.
        /// </summary>
        private void Start()
        {
            base.Start();

            // Auto-place target object if configured to do so
            if (StartWithObjectPlaced && ObjectToAutoPlace != null)
            {
                AutoPlaceObject();
            }
        }

        /// <summary>
        /// Auto-sets up the placement target with a specified Grabbable and model.
        /// </summary>
        /// <param name="grabbable">The Grabbable object to associate with this target.</param>
        /// <param name="modelGO">The GameObject representing the model to display.</param>
        public void AutoSetUp(Grabbable grabbable, GameObject modelGO)
        {
            // Set Name Filter and variables
            ObjectToAutoPlace = grabbable;
            NameFilter = true;
            Names = new string[1];
            Names[0] = grabbable.name;
            target = this.gameObject.transform;

            // Copy target transform from the Grabbable
            this.gameObject.transform.SetPositionAndRotation(grabbable.transform.position, grabbable.transform.rotation);
            this.gameObject.transform.localScale = grabbable.transform.localScale;

            // Setup Model GameObject, copying mesh and transform from Grabbable
            displayMesh = transform.Find("Model").gameObject;
            displayMesh.SetActive(true);
            displayMesh.transform.SetPositionAndRotation(modelGO.transform.position, modelGO.transform.rotation);
            displayMesh.transform.localScale = modelGO.transform.localScale;
            displayMesh.GetComponent<MeshFilter>().mesh = modelGO.GetComponent<MeshFilter>().sharedMesh;

            // Setup rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;

            // Add Collider
            Collider col = rb.GetComponent<Collider>();
            if (col == null)
                col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        /// <summary>
        /// Automatically places the associated Grabbable object onto the target.
        /// </summary>
        private void AutoPlaceObject()
        {
            // Grabbable
            ObjectToAutoPlace.pendingPlaceable = this;
            ObjectToAutoPlace.Place();

            // Placeable
            pendingPlacedObject = ObjectToAutoPlace.gameObject;
            Place();
        }

        /// <summary>
        /// Attaches the currently placed object to the target every frame.
        /// </summary>
        private void FixedUpdate()
        {
            // If this is a networked object and the local client is not its owner, don't set transform
            if (ObjectToAutoPlace.networkObject != null && !ObjectToAutoPlace.networkObject.IsOwner)
                return;

            if (placedObject != null)
            {
                // Attach the object manually each frame.
                placedObject.transform.position = this.target.position;
                placedObject.transform.rotation = this.target.rotation;
            }
            // Manage the ability to place the last placed object again
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
        /// <param name="other">The GameObject to check.</param>
        /// <returns>True if the object passes the filter; otherwise, false.</returns>
        private bool CheckNamesFilter(GameObject other)
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
        /// <param name="other">The GameObject to be placed.</param>
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

            if (preventPlacingAfterFirstPickUp)
                renderer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Checks if a specific object can be placed on the target based on current conditions.
        /// </summary>
        /// <param name="other">The GameObject to check for placement.</param>
        /// <returns>True if the object can be placed; otherwise, false.</returns>
        public bool CheckIfCanPlaceObject(GameObject other)
        {
            if (preventPlacingAfterFirstPickUp)
                return false;

            bool canPlace = pendingPlacedObject == null && placedObject == null;
            bool isLastObject = other == lastPlacedObject;
            return (canPlace && (!isLastObject || (isLastObject && canPlaceLastPlacedObject)) && CheckNamesFilter(other));
        }

        /// <summary>
        /// Wrapper to update the outline material from the Grabbable's OnTriggerStay.
        /// </summary>
        public void UpdatePlacing()
        {
            UpdateOutlineFill(displayMesh.GetComponent<Renderer>());
        }

        /// <summary>
        /// Handles user input for placement actions when the interaction type is set to BUTTON.
        /// </summary>
        private void Update()
        {
            if (pendingPlacedObject == null || interactionType != InteractionType.BUTTON)
                return;

            // Button Place
            if (Input.GetMouseButtonDown(0))
            {
                iTimer.OnFinishInteraction();
            }
        }

        /// <summary>
        /// Checks for voice commands related to object placement.
        /// </summary>
        /// <param name="_command">The voice command received from the user.</param>
        protected override void CheckVoiceCommand(string _command)
        {
            // Check all voice commands regardless of their VoiceCommandAction enum
            foreach (var command in voiceCommandKeywords)
            {
                // If not current language, skip to next voiceCommandKeyword
                if (command.language != LanguageController.Instance.currentLanguage)
                    continue;

                if (_command.Contains(command.keyword, StringComparison.InvariantCultureIgnoreCase))
                {
                    AutoPlaceObject();
                    break;
                }
            }
        }

        /// <summary>
        /// Automatically places the object and locks it to prevent further placements.
        /// </summary>
        public void AutoPlaceAndLock()
        {
            LockObjectOnPlacement = true;
            AutoPlaceObject();
        }
    }
}
