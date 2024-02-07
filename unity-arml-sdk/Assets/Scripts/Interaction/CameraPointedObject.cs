using UnityEngine;
using System;
using UltEvents;
using System.Collections.Generic;

#if UNITY_EDITOR
using TNRD.Utilities;
#endif

/// <summary>
/// Defines an interactable object that can be triggered based on the camera's angle, distance, and voice commands.
/// </summary>
public class CameraPointedObject : Interactable
{
    [Header("Camera Pointed Object")]
    [SerializeField] private float targetAngle = 5f;
    [SerializeField, Tooltip("Triggers unless you are looking at the object")]
    private bool invertAngleCheck = false;
    [SerializeField, Tooltip("Maximum distance in meters between camera and object allowed for interaction. 0 for infinite")]
    float maximumTriggerDistance = 5;
    [SerializeField] private bool checkObstaclesRaycast = false;
    [SerializeField] LayerMask blockingLayers = 0;

    [SerializeField, Tooltip("Once triggered, can you trigger it again immediately without looking away?")] bool canRetrigger = true;

    [Header("Trigger Collider")]
    [SerializeField, Tooltip("Collider used for trigger-based interaction. If null, uses angle-based interaction.")]
    private Collider triggerCollider;

    [Header("Event")]
    [SerializeField] protected UltEvent OnObjectInteractedEvent;

    [Tooltip("These Events will only fire if the game is currently in the specified Level")]
    [SerializeField] protected List<LevelEvent> levelFilterEvents;

    private LevelController levelController;

    // Re-triggering
    private bool alreadyTriggered = false;
    private bool triggerEntered = false;
    private Transform cachedTransform;
    private Transform mainCameraTransform;

#if UNITY_EDITOR
    /// <summary>
    /// Sets up the icon in the Unity Editor when this script is added to a GameObject.
    /// </summary>
    private void Reset()
    {
        base.Reset();
        IconManager.SetIcon(gameObject, LabelIcon.Purple);
    }
#endif

    /// <summary>
    /// Initializes the object, setting up references and voice command subscriptions.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        cachedTransform = transform;
        mainCameraTransform = Camera.main?.transform;
        levelController = FindObjectOfType<LevelController>();
    }

    protected override void CheckVoiceCommand(string command)
    {
        foreach (string keyword in voiceCommandKeywords)
        {
            if (command.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
            {
                OnObjectInteracted();
                break;
            }
        }
    }

    /// <summary>
    /// Regularly checks for interaction conditions based on camera angle and distance.
    /// </summary>
    void Update()
    {
        if (alreadyTriggered)
            UpdateOutlineFill(renderer);

        //If there's a TriggerCollider and OnTriggerEnter - avoid use of OnTriggerStay
        if (triggerCollider != null && triggerEntered)
        {
            CheckSuccessful();
            return;
        }

        if (!mainCameraTransform)
            mainCameraTransform = Camera.main.transform;

        if (IsBeyondInteractionDistance() || IsObstructedByRaycast()) return;

        HandleInteractionAngle();
    }

    /// <summary>
    /// Invokes the interaction event and plays feedback.
    /// </summary>
    protected void OnObjectInteracted()
    {
        //Call standard event
        OnObjectInteractedEvent?.Invoke();

        //Filter through LevelEvents to fire correct one
        if (!levelController) return;
        int currentLevel = levelController.currentLevel.levelIndex + 1;
        foreach (LevelEvent levelEvent in levelFilterEvents)
        {
            if (levelEvent.levelIndex == currentLevel)
                levelEvent.levelEvent?.Invoke();
        }

        //Fire feedback if any
        actionFeedback?.PlayRandomTriggerFeedback();
    }

    /// <summary>
    /// Subscribes to the finish interaction event.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        iTimer.OnFinishInteraction += OnObjectInteracted;
    }

    /// <summary>
    /// Unsubscribes from the finish interaction event.
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        iTimer.OnFinishInteraction -= OnObjectInteracted;

        if (interactionType == InteractionType.VOICE)
        {
            STTMicController.OnVoiceCommandAction -= CheckVoiceCommand;
        }
    }

    /// <summary>
    /// Checks if the object is beyond the maximum interaction distance.
    /// </summary>
    /// <returns>True if the object is beyond the maximum distance, false otherwise.</returns>
    private bool IsBeyondInteractionDistance()
    {
        if (maximumTriggerDistance == 0) return false;

        return Vector3.Distance(cachedTransform.position, mainCameraTransform.position) > maximumTriggerDistance;
    }

    /// <summary>
    /// Checks if the line of sight to the object is obstructed.
    /// </summary>
    /// <returns>True if the line of sight is obstructed, false otherwise.</returns>
    private bool IsObstructedByRaycast()
    {
        if (!checkObstaclesRaycast) return false;

        Vector3 direction = cachedTransform.position - mainCameraTransform.position;
        if (
            Physics.Raycast(mainCameraTransform.position, direction, out RaycastHit hit, direction.magnitude, blockingLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.gameObject != gameObject;
        }
        return false;
    }

    /// <summary>
    /// Handles the interaction logic based on the angle between the camera and the object.
    /// </summary>
    private void HandleInteractionAngle()
    {
        Vector3 direction = cachedTransform.position - mainCameraTransform.position;
        float angle = Vector3.Angle(mainCameraTransform.forward, direction);

        if (invertAngleCheck ? angle >= targetAngle : angle <= targetAngle)
        {
            CheckSuccessful();
        }
        else
        {
            CheckFail();
        }
    }

    /// <summary>
    /// Executes logic when the angle or collision check is successful, such as starting timers or invoking events.
    /// </summary>
    private void CheckSuccessful()
    {
        if (alreadyTriggered && !canRetrigger) return;

        switch (interactionType)
        {
            case InteractionType.DWELL:
                StartTimer();
                break;
            case InteractionType.BUTTON:
                if (Input.GetMouseButtonDown(0))
                    OnObjectInteracted();
                break;
        }

        if (outlineMaterial)
        {
            AddOutlineMaterial(renderer);
            if (interactionType == InteractionType.DWELL)
                actionFeedback?.PlayProgressFeedback();
        }

        alreadyTriggered = true;
    }

    /// <summary>
    /// Executes logic when the angle or collision check fails, such as stopping timers or removing visual feedback.
    /// </summary>
    private void CheckFail()
    {
        if (interactionType == InteractionType.DWELL)
            StopTimer();

        if (outlineMaterial)
        {
            RemoveOutlineMaterial(renderer);
            actionFeedback?.StopProgressFeedback();
        }

        alreadyTriggered = false;
    }

    /// <summary>
    /// Called when a Collider enters the trigger.
    /// This method checks if the entering Collider has a 'CameraGrabber' component.
    /// If it does, it treats the interaction as successful.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CameraGrabber>() == null)
            return;

        triggerEntered = true;
    }

    /// <summary>
    /// Called when a Collider exits the trigger.
    /// This method checks if the exiting Collider has a 'CameraGrabber' component.
    /// If it does, it treats the interaction as unsuccessful.
    /// </summary>
    /// <param name="other">The Collider that exited the trigger.</param>
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CameraGrabber>() == null)
            return;

        triggerEntered = false;
    }
}