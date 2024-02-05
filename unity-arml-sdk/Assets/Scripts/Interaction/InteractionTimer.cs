using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a timer for interactions, updating a UI element to reflect the progress and triggering events upon completion or cancellation.
/// </summary>
public class InteractionTimer : MonoBehaviour
{
    [field: Header("Interaction Time")]
    [field: SerializeField]
    public float RequiredInteractionTime { get; set; } = 1.5f;
    [field: SerializeField]
    public float CurrentInteractionTime { get; private set; } = 0f;

    public float InteractionPercent { get { return CurrentInteractionTime / RequiredInteractionTime; } }

    [field: Header("Scaling")]
    [field: SerializeField]
    public float growthScale { get; private set; } = 1f;
    [field: SerializeField]
    public float shrinkScale { get; private set; } = 1.5f;

    [Header("UI elements")]
    public GameObject UI;
    public RectTransform panel;
    public Image fillIndicator;
    public TextMeshProUGUI stateText;
    private Camera cam;

    public bool hideTimerUI = true;

    public bool IsInteracting { get; private set; } = false;
    public bool IsResting { get; private set; } = true;
    public Action OnFinishInteraction;
    public Action OnCancelInteraction;

    /// <summary>
    /// Initializes the timer and subscribes to the necessary events.
    /// </summary>
    void Start()
    {
        NetworkPlayer.OnPlayerLoaded += GetCamera;
    }

    private void OnDisable()
    {
        NetworkPlayer.OnPlayerLoaded -= GetCamera;
    }

    /// <summary>
    /// Retrieves the main camera for UI positioning.
    /// </summary>
    void GetCamera()
    {
        cam = Camera.main;
    }

    /// <summary>
    /// Updates the interaction timer and UI each frame.
    /// </summary>
    void LateUpdate()
    {
        if (IsResting)
            return;

        if (IsInteracting)
        {
            CurrentInteractionTime += growthScale * Time.deltaTime;

            if (CurrentInteractionTime > RequiredInteractionTime)
            {
                OnFinishInteraction?.Invoke();
                IsInteracting = false;
                ResetInteraction();
            }
        }
        else
        {
            CurrentInteractionTime -= shrinkScale * Time.deltaTime;
            if (CurrentInteractionTime <= 0)
            {
                ResetInteraction();
            }
        }
        UpdateUI();
    }

    /// <summary>
    /// Starts the interaction, showing and updating the UI.
    /// </summary>
    public void StartInteraction()
    {
        IsResting = false;
        IsInteracting = true;
        UI.SetActive(true);
        UI.GetComponent<Canvas>().enabled = !hideTimerUI;
    }

    /// <summary>
    /// Cancels the current interaction and invokes any cancellation actions.
    /// </summary>
    public void CancelInteraction()
    {
        OnCancelInteraction?.Invoke();
        IsInteracting = false;
    }

    /// <summary>
    /// Resets the interaction timer and hides the UI.
    /// </summary>
    private void ResetInteraction()
    {
        IsResting = true;
        CurrentInteractionTime = 0f;
        UI.SetActive(false);
    }

    /// <summary>
    /// Updates the UI elements to reflect the current interaction progress.
    /// </summary>
    private void UpdateUI()
    {
        // Uncomment the following lines if you need to position the panel based on the world position.
        // if (!cam) return;
        // panel.position = cam.WorldToScreenPoint(this.transform.position);

        fillIndicator.fillAmount = InteractionPercent;
    }
}
