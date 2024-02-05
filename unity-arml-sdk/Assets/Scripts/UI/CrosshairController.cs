using UnityEngine;
using UnityEngine.UI;

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
        CANPLACE
    }

    private CrosshairState currentState;
    private CrosshairState previousState;

    [SerializeField] Sprite idleCrosshair;
    [SerializeField] Sprite canGrabCrosshair;
    [SerializeField] Sprite actionCrosshair;
    [SerializeField] Sprite canPlaceCrosshair;

    private Image image;
    private Grabbable currentlyGrabbedObject;

    void OnEnable()
    {
        Grabbable.OnStartGrabbingAttemptAction += ChangeToCanGrab;
        Grabbable.OnStopGrabbingAttemptAction += ChangeToIdle;
        Grabbable.OnGrabSuccesfulAction += ChangeToCanDoAction;
        PlacementTarget.OnStartPlacingAttemptAction += ChangeToCanPlace;
        PlacementTarget.OnStopPlacingAttemptAction += ChangeToPreviousState;
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
    private void ChangeToIdle()
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
    /// Changes the state of the crosshair and updates its sprite accordingly.
    /// </summary>
    /// <param name="newState">The new state to change the crosshair to.</param>
    public void ChangeCrosshairState(CrosshairState newState)
    {
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
        }
    }

    /// <summary>
    /// Reverts the crosshair to its previous state.
    /// </summary>
    private void ChangeToPreviousState()
    {
        currentState = previousState;
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
