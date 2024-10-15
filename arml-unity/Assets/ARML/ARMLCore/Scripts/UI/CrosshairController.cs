using ARML.DS;
using UnityEngine;
using UnityEngine.UI;
using ARML.Voice;
using ARML.Interaction;

namespace ARML.UI
{
    /// <summary>
    /// Controls the crosshair's appearance and state based on different game interactions like grabbing or placing objects.
    /// </summary>
    public class CrosshairController : MonoBehaviour
    {

        public enum CrosshairState
        {
            IDLE,
            CANGRAB,
            CANDOACTION,
            CANPLACE,
            CANTALK,
            CANRECORD
        }

        private CrosshairState currentState;
        [HideInInspector] public CrosshairState previousState;

        [SerializeField] Sprite idleCrosshair;
        [SerializeField] Sprite canGrabCrosshair;
        [SerializeField] Sprite actionCrosshair;
        [SerializeField] Sprite canPlaceCrosshair;
        [SerializeField] Sprite questionMarkCrosshair;
        [SerializeField] Sprite microphoneCrosshair;

        [SerializeField] STTMicController stTMicController;

        private Image image;
        private Grabbable currentlyGrabbedObject;

        public static CrosshairController Instance;

        private void Awake()
        {
            Singleton();
        }

        private void Singleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy the GameObject if an instance already exists
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Optionally make it persistent
            }
        }

        void OnEnable()
        {
            Grabbable.OnStartGrabbingAttemptAction += ChangeToCanGrab;
            Grabbable.OnStopGrabbingAttemptAction += ChangeToIdle;
            Grabbable.OnGrabSuccesfulAction += ChangeToCanDoAction;

            PlacementTarget.OnStartPlacingAttemptAction += ChangeToCanPlace;
            PlacementTarget.OnStopPlacingAttemptAction += ChangeToPreviousState;

            DSDialogue.OnStartTalkingAttemptAction += ChangeToCanTalk;
            DSDialogue.OnStopTalkingAttemptAction += ChangeToPreviousState;

            DSDialogue.OnStartRecordingAttemptAction += ChangeToCanRecord;
            DSDialogue.OnStopRecordingAttemptAction += ChangeToPreviousState;

            CameraPointedObject.OnCheckSuccesfulAction += ChangeToOverridenCrosshair;
            CameraPointedObject.OnCheckFailAction += ChangeToPreviousState;
        }

        void OnDisable()
        {
            Grabbable.OnStartGrabbingAttemptAction -= ChangeToCanGrab;
            Grabbable.OnStopGrabbingAttemptAction -= ChangeToIdle;
            Grabbable.OnGrabSuccesfulAction -= ChangeToCanDoAction;
            PlacementTarget.OnStartPlacingAttemptAction -= ChangeToCanPlace;
            PlacementTarget.OnStopPlacingAttemptAction -= ChangeToPreviousState;
        }

        /// <summary>
        /// Initializes the controller and sets the default crosshair sprite.
        /// </summary>
        private void Start()
        {
            image = GetComponent<Image>();
            ChangeChrosshairSprite();
        }

        /// <summary>
        /// Changes the crosshair state to 'Idle'.
        /// </summary>
        public void ChangeToIdle()
        {
            ChangeCrosshairState(CrosshairState.IDLE);
        }

        /// <summary>
        /// Changes the crosshair state to 'CanGrab'.
        /// </summary>
        private void ChangeToCanGrab()
        {
            ChangeCrosshairState(CrosshairState.CANGRAB);
        }

        /// <summary>
        /// Changes the crosshair state to 'CanDoAction' when an object is successfully grabbed.
        /// </summary>
        /// <param name="grabbable">The object that has been grabbed.</param>
        private void ChangeToCanDoAction(Grabbable grabbable)
        {
            currentlyGrabbedObject = grabbable;
            currentlyGrabbedObject.OnPlace += OnPlaceObject;
            ChangeCrosshairState(CrosshairState.CANDOACTION);
        }

        /// <summary>
        /// Changes the crosshair state to 'CanPlace'.
        /// </summary>
        private void ChangeToCanPlace()
        {
            ChangeCrosshairState(CrosshairState.CANPLACE);
        }

        /// <summary>
        /// Changes the crosshair state to 'CanTalk'.
        /// </summary>
        private void ChangeToCanTalk()
        {
            ChangeCrosshairState(CrosshairState.CANTALK);
        }

        private void ChangeToCanRecord()
        {
            ChangeCrosshairState(CrosshairState.CANRECORD);
        }

        private void ChangeToOverridenCrosshair(CrosshairState overridenCrosshair)
        {
            ChangeCrosshairState(overridenCrosshair);
        }

        /// <summary>
        /// Changes the state of the crosshair and updates its sprite accordingly.
        /// </summary>
        /// <param name="newState">The new state to change the crosshair to.</param>
        public void ChangeCrosshairState(CrosshairState newState)
        {
            if (newState != currentState)
                previousState = currentState;

            currentState = newState;
            ChangeChrosshairSprite();
        }

        /// <summary>
        /// Updates the crosshair sprite based on the current state.
        /// </summary>
        private void ChangeChrosshairSprite()
        {
            switch (currentState)
            {
                case CrosshairState.IDLE:
                    image.sprite = idleCrosshair;
                    break;
                case CrosshairState.CANGRAB:
                    image.sprite = canGrabCrosshair;
                    break;
                case CrosshairState.CANDOACTION:
                    image.sprite = actionCrosshair;
                    break;
                case CrosshairState.CANPLACE:
                    image.sprite = canPlaceCrosshair;
                    break;
                case CrosshairState.CANTALK:
                    image.sprite = questionMarkCrosshair;
                    break;
                case CrosshairState.CANRECORD:
                    image.sprite = microphoneCrosshair;
                    break;
            }
        }

        /// <summary>
        /// Reverts the crosshair to its previous state.
        /// </summary>
        public void ChangeToPreviousState()
        {
            if (stTMicController.currentlyRecording) return;
            if (currentState == CrosshairState.IDLE) return;

            if (stTMicController.voiceCommandMode)
                //ChangeCrosshairState(CrosshairState.CANRECORD);

                //Horrible hard coding but need it for dwell -> CanRecord approach
                if (currentState == CrosshairState.CANRECORD && previousState == CrosshairState.IDLE)
                {
                    if (stTMicController.fromDwellToRecord)
                    {
                        stTMicController.fromDwellToRecord = false;
                        return;
                    }
                }

            var _state = currentState;
            currentState = previousState;

            previousState = _state;

            ChangeChrosshairSprite();
        }

        /// <summary>
        /// Handles actions when an object is placed, changing the crosshair to 'Idle'.
        /// </summary>
        private void OnPlaceObject()
        {
            currentlyGrabbedObject.OnPlace -= ChangeToIdle; //Important to unsubscribe
            ChangeToIdle();
        }
    }
}