using AClockworkBerry;
using UnityEngine;

/// <summary>
/// Manages the visibility of debug elements on the canvas, allowing them to be toggled on and off.
/// </summary>
public class DebugCanvasController : MonoBehaviour
{
    public bool freePlacementMode = false;
    public GameObject debugPlane; // Add a public reference to the debug plane GameObject

    [SerializeField] ScreenLogger logger;

    /// <summary>
    /// Initializes the DebugCanvasController. In builds outside of the Unity editor, it automatically hides the debug elements.
    /// </summary>
    private void Start()
    {
#if !UNITY_EDITOR
        ToggleElements();
#endif
    }

    /// <summary>
    /// Called once per frame. Checks for input to toggle the visibility of debug elements.
    /// </summary>
    void Update()
    {
        // Toggle Debug elements On/Off
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleElements();
        }
    }

    /// <summary>
    /// Toggles the visibility of each child element in the canvas and the screen logger.
    /// </summary>
    private void ToggleElements()
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(!child.gameObject.activeInHierarchy);

        logger.ShowLog = !logger.ShowLog;

        //freePlacementMode = !freePlacementMode;
    }
}