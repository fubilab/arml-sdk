using AClockworkBerry;
using UnityEngine;
using UnityEngine.EventSystems;
using ARML.Interaction;

namespace ARML.DebugTools
{
    /// <summary>
    /// Manages the visibility of debug elements on the canvas, allowing them to be toggled on and off.
    /// </summary>
    public class DebugCanvasController : MonoBehaviour
    {
        [SerializeField] GameObject debugPanel;
        [SerializeField] GameObject trackingText;
        [SerializeField] GameObject micInputText;
        [SerializeField] CameraParentController camParentController;
        [SerializeField] GameObject fpsText;

        private ToggleMapRenderer mapRenderer;
        private EventSystem eventSystem;

        /// <summary>
        /// Initializes the DebugCanvasController. In builds outside of the Unity editor, it automatically hides the debug elements.
        /// </summary>
        private void Start()
        {
            eventSystem = FindObjectOfType<EventSystem>();

            if (debugPanel.gameObject.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(debugPanel.transform.GetChild(0).gameObject);
            }

            camParentController.allowMove = !debugPanel.gameObject.activeInHierarchy;

#if !UNITY_EDITOR
        //Cursor.lockState = CursorLockMode.Locked;
#endif

#if UNITY_EDITOR
            ToggleAllDebug();
#endif
        }

        /// <summary>
        /// Called once per frame. Checks for input to toggle the visibility of debug elements.
        /// </summary>
        void Update()
        {
            // Toggle Debug Panel
            if (Input.GetKeyDown(KeyCode.Q))
            {
                ToggleAllDebug();
            }
        }

        public void ToggleAllDebug()
        {
            debugPanel.SetActive(!debugPanel.gameObject.activeInHierarchy);
            trackingText.SetActive(!trackingText.gameObject.activeInHierarchy);
            micInputText.SetActive(!micInputText.activeInHierarchy);
            fpsText.SetActive(!fpsText.activeInHierarchy);

            if (debugPanel.gameObject.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(debugPanel.transform.GetChild(0).gameObject);
            }

            camParentController.allowMove = !debugPanel.gameObject.activeInHierarchy;

#if !UNITY_EDITOR
        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = debugPanel.gameObject.activeInHierarchy;
#endif
        }

        public void ToggleScreenLogger()
        {
            ScreenLogger.Instance.ShowLog = !ScreenLogger.Instance.ShowLog;
        }

        public void SetScreenLogger(bool state)
        {
            ScreenLogger.Instance.ShowLog = state;
        }

        public void ToggleGOActivate(GameObject go)
        {
            go.SetActive(!go.activeInHierarchy);
        }

        public void ToggleMapRenderer()
        {
            if (mapRenderer == null)
                mapRenderer = FindObjectOfType<ToggleMapRenderer>();

            mapRenderer.Toggle();
        }

        public void SetMapRenderer(bool state)
        {
            if (mapRenderer == null)
                mapRenderer = FindObjectOfType<ToggleMapRenderer>();

            mapRenderer.SetRenderer(state);
        }
    }
}