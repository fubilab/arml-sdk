using DS;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ARML
{
    /// <summary>
    /// An abstract class representing an object that can be interacted with in various ways, such as grabbing and placing.
    /// This class provides the foundation for creating grabbable objects in a 3D environment, handling physics interactions,
    /// state management, and voice commands.
    /// </summary>
    public abstract class Grabbable : Interactable
    {
        /// <summary>
        /// Enumeration representing the possible states of the grabbable object.
        /// </summary>
        public enum GrabbableState { FREE, GRABBED, PLACED, LOCKED }

        [field: Header("Physics")]
        /// <summary>
        /// The Rigidbody component associated with the grabbable object.
        /// </summary>
        public Rigidbody rb { get; protected set; }
        protected bool usesGravity;

        // ---- States
        [HideInInspector]
        /// <summary>
        /// The current state of the grabbable object.
        /// </summary>
        public GrabbableState state { get; protected set; } = GrabbableState.FREE;

        [HideInInspector]
        /// <summary>
        /// The previous state of the grabbable object, used for state transition logic.
        /// </summary>
        public GrabbableState previousState { get; protected set; } = GrabbableState.FREE;

        [field: Header("Grab")]
        /// <summary>
        /// The target where the grabbable object can be placed.
        /// </summary>
        public PlacementTarget placeable { get; protected set; }

        /// <summary>
        /// The pending target for placement, set when attempting to place the object.
        /// </summary>
        public PlacementTarget pendingPlaceable { get; set; }

        [field: SerializeField]
        /// <summary>
        /// The transform that the grabbable object will follow when grabbed.
        /// </summary>
        protected Transform grabTarget;

        protected bool attemptingToGrab { get; private set; } = false;

        [field: SerializeField]
        /// <summary>
        /// The scale factor for smooth movement when grabbing and placing the object.
        /// </summary>
        protected float lerpScale { get; private set; } = 7.0f;

        [Header("Collider Scaling")]
        [SerializeField, Tooltip("Multiplies the collider size by this amount when grabbed.")]
        /// <summary>
        /// Multiplier for the collider size when the object is grabbed, useful for adjusting interaction distance.
        /// </summary>
        private Vector3 grabColliderScaleMultiplier = new Vector3(1f, 1f, 1f);

        private Vector3 originalColliderSize;

        /// <summary>
        /// The BoxCollider component of the grabbable object.
        /// </summary>
        public BoxCollider grabbableCollider;

        [field: Header("Event")]
        [SerializeField]
        /// <summary>
        /// Event invoked when the object is grabbed.
        /// </summary>
        protected UnityEvent OnObjectGrabbedEvent;

        [field: Tooltip("This event is called when the button is pressed WHILE the Grabbable is currently being grabbed.")]
        [SerializeField]
        /// <summary>
        /// Event invoked when the button is pressed while the object is grabbed.
        /// </summary>
        protected UnityEvent OnButtonDownWhileGrabbedEvent;

        [SerializeField]
        /// <summary>
        /// Event invoked when the button is released while the object is grabbed.
        /// </summary>
        protected UnityEvent OnButtonUpWhileGrabbedEvent;

        // ---- Grab subscribable void actions
        /// <summary>
        /// Action invoked when a grabbing attempt starts.
        /// </summary>
        public static Action OnStartGrabbingAttemptAction;

        /// <summary>
        /// Action invoked when a grabbing attempt stops.
        /// </summary>
        public static Action OnStopGrabbingAttemptAction;

        /// <summary>
        /// Action invoked when an object is successfully grabbed. Provides a reference to the grabbed object.
        /// </summary>
        public static Action<Grabbable> OnGrabSuccesfulAction;

        /// <summary>
        /// Action invoked when the object is placed.
        /// </summary>
        public Action OnPlace;

        /// <summary>
        /// Action invoked when the object is released.
        /// </summary>
        public static Action OnReleaseAction;

        protected Camera cam;
        protected bool canDoAction = true;
        protected AutoHideMaterials autoHideMaterials;

        /// <summary>
        /// Initializes the grabbable object, setting up necessary components and states.
        /// </summary>
        protected virtual void Awake()
        {
            base.Awake();
            rb = model?.GetComponent<Rigidbody>();
            usesGravity = rb.useGravity;
            cam = Camera.main;

            grabbableCollider = GetComponent<BoxCollider>();
            if (grabbableCollider != null)
            {
                // Store the original collider size
                originalColliderSize = grabbableCollider.size;
            }

            autoHideMaterials = GetComponent<AutoHideMaterials?>();
        }

        private void Start()
        {
            DSDialogue.OnStartTalkingAttemptAction += HideGrabbable;
            DSDialogue.OnStopTalkingAttemptAction += UnhideGrabbable;
        }

        private void HideGrabbable()
        {
            if (state != GrabbableState.GRABBED)
                return;

            canDoAction = false;
            //model.SetActive(false);
            if (autoHideMaterials != null)
                autoHideMaterials.MakeMaterialsTransparent();
        }

        private void UnhideGrabbable()
        {
            if (state != GrabbableState.GRABBED)
                return;

            //model.SetActive(true);
            canDoAction = true;
            if (autoHideMaterials != null)
                autoHideMaterials.RestoreMaterials();
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
                StartTimer();
            //iTimer.StartInteraction();

            //Set outline material
            if (outlineMaterial != null)
                AddOutlineMaterial(renderer);

            //Play hover sfx
            actionFeedback?.PlayHoverSFX();

            this.grabTarget = target;
            if (OnStartGrabbingAttemptAction != null)
                OnStartGrabbingAttemptAction();

            //Log to CSV Export
            MonitoredAction monitoredAction = new MonitoredAction()
            {
                ActionType = MonitoredAction.ActionTypeEnum.HOVER,
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ActionObject = this.gameObject.name
            };

            CSVExport.Instance?.monitoredActions.Add(monitoredAction);

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
                //grabbableCollider.size = originalColliderSize.Multiply(grabColliderScaleMultiplier);
            }

            //Log to CSV Export
            MonitoredAction monitoredAction = new MonitoredAction()
            {
                ActionType = MonitoredAction.ActionTypeEnum.GRABBED,
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ActionObject = this.gameObject.name
            };

            CSVExport.Instance?.monitoredActions.Add(monitoredAction);
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

            //Log to CSV Export
            MonitoredAction monitoredAction = new MonitoredAction()
            {
                ActionType = MonitoredAction.ActionTypeEnum.UNHOVER,
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ActionObject = this.gameObject.name
            };

            CSVExport.Instance?.monitoredActions.Add(monitoredAction);
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
        public virtual void Place()
        {
            placeable = pendingPlaceable;
            pendingPlaceable = null;
            grabTarget = null;

            UpdateState(state, placeable.LockObjectOnPlacement ? GrabbableState.LOCKED : GrabbableState.PLACED);
            this.gameObject.layer = LayerMask.NameToLayer("Grabbable");

            rb.isKinematic = true;

            if (placeable.SmoothPlacement)
            {
                // Start the coroutine for a smooth transition
                if (gameObject.activeInHierarchy)
                    StartCoroutine(MoveToPosition(placeable.target.transform, placeable.SmoothPlacementSpeed));
            }
            else
            {
                rb.MovePosition(placeable.target.transform.position);
                rb.MoveRotation(placeable.target.transform.rotation);
            }

            interactionTimer.CancelInteraction();

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

        private IEnumerator MoveToPosition(Transform target, float speed)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = target.position;
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = target.rotation;

            float distance = Vector3.Distance(startPosition, endPosition);
            float duration = distance / speed;
            float time = 0;

            while (time < duration)
            {
                transform.position = Vector3.Lerp(startPosition, endPosition, time / duration);
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, time / duration);
                time += Time.deltaTime;
                yield return null;
            }

            transform.position = endPosition;
            transform.rotation = endRotation;
        }

        /// <summary>
        /// Removes the object from its placement.
        /// </summary>
        public void RemoveFromPlace()
        {
            //rb.isKinematic = false;
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
                if (!canDoAction) return;

                //Button Grab
                if (attemptingToGrab && interactionType == InteractionType.BUTTON)
                {
                    iTimer.OnFinishInteraction();
                    return; //This prevents invoking the ButtonDown event immediately after
                }

                if (state == GrabbableState.GRABBED && interactionType == InteractionType.BUTTON)
                {
                    OnButtonDownWhileGrabbedEvent?.Invoke();
                    actionFeedback?.PlayRandomTriggerFeedback();

                    //Log to CSV Export
                    MonitoredAction monitoredAction = new MonitoredAction()
                    {
                        ActionType = MonitoredAction.ActionTypeEnum.USED,
                        TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ActionObject = this.gameObject.name
                    };

                    CSVExport.Instance?.monitoredActions.Add(monitoredAction);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (!canDoAction) return;

                if (state == GrabbableState.GRABBED && interactionType == InteractionType.BUTTON)
                {
                    OnButtonUpWhileGrabbedEvent?.Invoke();
                }
            }
        }

        protected override void CheckVoiceCommand(string incomingKeyword)
        {
            //Filter through commands based on their VoiceCommandAction
            foreach (VoiceCommandKeyword command in voiceCommandKeywords)
            {
                //If not current language, skip to next voiceCommandKeyword
                if (command.language != LanguageController.Instance.currentLanguage)
                    continue;

                if (incomingKeyword.Contains(command.keyword, StringComparison.InvariantCultureIgnoreCase))
                {
                    switch (command.action)
                    {
                        case VoiceCommandAction.GRAB_OR_PLACE:
                            if (attemptingToGrab)
                                iTimer.OnFinishInteraction();
                            //else if(state == GrabbableState.GRABBED)
                            //TODO Add Place logic here
                            break;
                        case VoiceCommandAction.USE:
                            if (state == GrabbableState.GRABBED)
                                OnButtonDownWhileGrabbedEvent?.Invoke();
                            break;
                        case VoiceCommandAction.FREE:
                            command.FreeActionEvent?.Invoke();
                            break;
                    }
                }
            }
        }
    }
}