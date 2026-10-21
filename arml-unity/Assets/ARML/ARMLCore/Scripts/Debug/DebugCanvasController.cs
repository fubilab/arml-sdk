using AClockworkBerry;
using ARML.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ARML.Interaction;
using TMPro;

namespace ARML.DebugTools
{
    /// <summary>
    /// Manages the visibility of debug elements on the canvas, allowing them to be toggled on and off.
    /// </summary>
    public class DebugCanvasController : MonoBehaviour
    {
        [Header("Settings"), RuntimeParameter] 
        [SerializeField] private bool showFPSCounterAlways = false;
        
        [Header("References")]
        [SerializeField] GameObject debugPanel;
        [SerializeField] SettingsPanel settingsPanel;
        [SerializeField] GameObject trackingText;
        [SerializeField] GameObject micInputText;
        [SerializeField] CameraParentController camParentController;
        [SerializeField] TMP_Text fpsText;

        private ToggleMapRenderer[] mapRenderers = System.Array.Empty<ToggleMapRenderer>();
        private EventSystem eventSystem;
        private MenuState _currentMenuState = MenuState.DEBUG;
        private bool menuStateInitialized;
        private bool remoteControlSubscribed;

        private enum MenuState
        {
            NONE,
            DEBUG,
            PARAMETERS
        }

        /// <summary>
        /// Sets the initial menu state to the debug panel in player builds and closed panels in the editor.
        /// </summary>
        private void Awake()
        {
            if (debugPanel == null || settingsPanel == null || trackingText == null ||
                micInputText == null || camParentController == null || fpsText == null)
            {
                Debug.LogError($"{nameof(DebugCanvasController)} is missing one or more required Inspector references.", this);
                enabled = false;
                return;
            }

            eventSystem = EventSystem.current != null
                ? EventSystem.current
                : FindFirstObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                Debug.LogWarning($"{nameof(DebugCanvasController)} could not find an EventSystem; debug-panel selection will be unavailable.", this);
            }

            RefreshMapRenderers();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SubscribeToRemoteControl();
        }

        private void Start()
        {
            if (!SubscribeToRemoteControl())
            {
                Debug.LogWarning($"{nameof(DebugCanvasController)} could not find a {nameof(RemoteControl)}; the remote menu button will be unavailable.", this);
            }

            if (menuStateInitialized)
            {
                return;
            }

#if UNITY_EDITOR
            SetMenuState(MenuState.NONE);
#else
            SetMenuState(MenuState.DEBUG);
#endif
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (remoteControlSubscribed && RemoteControl.Instance != null)
            {
                RemoteControl.Instance.OnMenuPress -= ToggleAllDebug;
            }
            remoteControlSubscribed = false;
        }

        private bool SubscribeToRemoteControl()
        {
            if (remoteControlSubscribed)
            {
                return true;
            }

            if (RemoteControl.Instance == null)
            {
                return false;
            }

            RemoteControl.Instance.OnMenuPress += ToggleAllDebug;
            remoteControlSubscribed = true;
            return true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshMapRenderers();
        }

        private void RefreshMapRenderers()
        {
            mapRenderers = FindObjectsByType<ToggleMapRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        public void ToggleAllDebug()
        {
            SetMenuState(_currentMenuState == MenuState.NONE ? MenuState.DEBUG : MenuState.NONE);
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
            if (go == null)
            {
                Debug.LogError($"{nameof(ToggleGOActivate)} received a null GameObject.", this);
                return;
            }

            go.SetActive(!go.activeSelf);
        }

        public void ToggleMapRenderer()
        {
            foreach (var renderer in mapRenderers)
            {
                if (renderer != null)
                {
                    renderer.Toggle();
                }
            }
        }

        public void SetMapRenderer(bool state)
        {
            foreach (var renderer in mapRenderers)
            {
                if (renderer != null)
                {
                    renderer.SetRenderer(state);
                }
            }
        }

        public void SetParametersPanel()
        {
            SetMenuState(MenuState.PARAMETERS);
        }

        public void SetDebugPanel()
        {
            SetMenuState(MenuState.DEBUG);
        }

        public void ClosePanels()
        {
            SetMenuState(MenuState.NONE);
        }

        private void SetMenuState(MenuState state)
        {
            _currentMenuState = state;
            menuStateInitialized = true;

            settingsPanel.SetPanelVisibility(state == MenuState.PARAMETERS);
            debugPanel.SetActive(state == MenuState.DEBUG);

            bool showStatus = state != MenuState.NONE;
            trackingText.SetActive(showStatus);
            micInputText.SetActive(showStatus);
            
            fpsText.enabled = showFPSCounterAlways || showStatus;

            camParentController.allowMove = state == MenuState.NONE;

            if (state == MenuState.DEBUG && debugPanel.activeInHierarchy &&
                eventSystem != null && debugPanel.transform.childCount > 0)
            {
                eventSystem.SetSelectedGameObject(debugPanel.transform.GetChild(0).gameObject);
            }
            else if (state == MenuState.NONE && eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }

#if !UNITY_EDITOR
            Cursor.lockState = state == MenuState.NONE ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = state != MenuState.NONE;
#endif
        }
    }
}