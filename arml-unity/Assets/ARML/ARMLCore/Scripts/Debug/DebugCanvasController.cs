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
        [SerializeField] SettingsPanel settingsPanel;
        [SerializeField] GameObject trackingText;
        [SerializeField] GameObject micInputText;
        [SerializeField] CameraParentController camParentController;
        [SerializeField] GameObject fpsText;

        private ToggleMapRenderer mapRenderer;
        private EventSystem eventSystem;
        private MenuState _currentMenuState = MenuState.DEBUG;

        private enum MenuState
        {
            NONE,
            DEBUG,
            PARAMETERS
        }

        /// <summary>
        /// Initializes the DebugCanvasController. In builds outside of the Unity editor, it automatically hides the debug elements.
        /// </summary>
        private void Start()
        {
            // bind remote control menu button to debug canvas
            RemoteControl.Instance.OnMenuPress = ToggleAllDebug;

            // Get event system reference
            eventSystem = FindObjectOfType<EventSystem>();

#if !UNITY_EDITOR
            SetDebugPanel();
#endif

#if UNITY_EDITOR
            ClosePanels();
#endif
        }

        public void ToggleAllDebug()
        {
        
            switch(_currentMenuState)
            {
                case MenuState.NONE:
                    SetDebugPanel();
                    break;
                case MenuState.DEBUG:
                    ClosePanels();
                    break;
                case MenuState.PARAMETERS:
                    ClosePanels();
                    break;
            }

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
            foreach(var m in FindObjectsByType<ToggleMapRenderer>(FindObjectsSortMode.None))
                m.Toggle();
        }

        public void SetMapRenderer(bool state)
        {
            foreach(var renderer in FindObjectsByType<ToggleMapRenderer>(FindObjectsSortMode.None))
                renderer.SetRenderer(state);
        }
        
        public void SetParametersPanel()
        {
            _currentMenuState = MenuState.PARAMETERS;
            
            settingsPanel.SetPanelVisibility(true);
            
            debugPanel.SetActive(false);
            trackingText.SetActive(true);
            micInputText.SetActive(true);
            fpsText.SetActive(true);
            
            camParentController.allowMove = false;
        }
        
        public void SetDebugPanel()
        {
            _currentMenuState = MenuState.DEBUG;
            
            settingsPanel.SetPanelVisibility(false);
            
            debugPanel.SetActive(true);
            trackingText.SetActive(true);
            micInputText.SetActive(true);
            fpsText.SetActive(true);
            
            camParentController.allowMove = false;
            
            if (debugPanel.gameObject.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(debugPanel.transform.GetChild(0).gameObject);
            }
        }

        public void ClosePanels()
        {
            _currentMenuState = MenuState.NONE;
            
            settingsPanel.SetPanelVisibility(false);
            
            debugPanel.SetActive(false);
            trackingText.SetActive(false);
            micInputText.SetActive(false);
            fpsText.SetActive(false);
            
            camParentController.allowMove = true;
        }
    }
}