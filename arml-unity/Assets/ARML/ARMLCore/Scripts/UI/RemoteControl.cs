using System;
using System.Collections;
using ARML.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public class RemoteControl : MonoBehaviour {
    public static RemoteControl Instance;

    /// <summary>
    /// Ensures only one instance of RemoteControl exists.
    /// </summary>
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

    [Tooltip("Time in seconds for long press to activate")]
    public float LongPressTime = 3;

    [Tooltip("Menu key assignment")]
    public KeyCode MenuKey = KeyCode.Menu;
    [Tooltip("Alt menu key assignment")]
    public KeyCode MenuKey2 = KeyCode.PageUp;

    [Tooltip("Reset pose key assignment (for systems that do not support long press).")]
    public KeyCode ResetKey = KeyCode.Backspace;

    public bool ReadLauncherSettings = true;

    public Action OnMenuPress;
    public Action OnMenuLongPress;

    IEnumerator _MenuButtonPressCoroutine = null;

    IEnumerator MenuButtonPress() {
        yield return new WaitForSeconds(LongPressTime);
        OnMenuLongPress?.Invoke();
        // print("Menu long press");
        _MenuButtonPressCoroutine = null;
    }

    void Awake() 
    {
        Singleton();
    }

    void Start()
    {
        if (ReadLauncherSettings)
        {
            var launcherSettings = SettingsConfiguration.LoadFromDisk();
            MenuKey = launcherSettings.menuKey;
            MenuKey2 = launcherSettings.menuKey2;
            ResetKey = launcherSettings.resetKey;
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(MenuKey) || Input.GetKeyDown(MenuKey2))
        {
            //print("Down: Menu");
            _MenuButtonPressCoroutine = MenuButtonPress();
            StartCoroutine(_MenuButtonPressCoroutine);
        }
        if (Input.GetKeyUp(MenuKey) || Input.GetKeyUp(MenuKey2))
        {
            if (_MenuButtonPressCoroutine != null) {
                StopCoroutine(_MenuButtonPressCoroutine);
                _MenuButtonPressCoroutine = null;
                // print("Up: Menu");
                OnMenuPress?.Invoke();
            }
        }

        if (Input.GetKeyDown(ResetKey))
        {
            OnMenuLongPress?.Invoke();
        }
    }

    // ---------------------------------------------------------------- software input
    // The same actions as the hardware remote, for code that has no physical keys to press
    // (e.g. the OSC remote running on a phone).

    /// <summary>Same as a short press of the menu button: opens / closes the debug menu.</summary>
    public static bool PressMenu()
    {
        if (Instance == null)
        {
            Debug.LogWarning("RemoteControl: no instance in the scene, cannot press the menu button.");
            return false;
        }
        Instance.OnMenuPress?.Invoke();
        return true;
    }

    /// <summary>Same as the reset key / a long press of the menu button: resets the pose.</summary>
    public static bool PressReset()
    {
        if (Instance == null)
        {
            Debug.LogWarning("RemoteControl: no instance in the scene, cannot press the reset button.");
            return false;
        }
        Instance.OnMenuLongPress?.Invoke();
        return true;
    }

    /// <summary>
    /// Same as pressing an arrow key: moves the UI selection (or changes a slider) on the currently
    /// selected element. Like the keyboard, it does nothing while nothing is selected.
    /// </summary>
    public static void Navigate(MoveDirection direction)
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        if (selected == null) return;

        Vector2 vector;
        switch (direction)
        {
            case MoveDirection.Left: vector = Vector2.left; break;
            case MoveDirection.Right: vector = Vector2.right; break;
            case MoveDirection.Up: vector = Vector2.up; break;
            case MoveDirection.Down: vector = Vector2.down; break;
            default: return;
        }

        // This is what the input module sends for an arrow key.
        var data = new AxisEventData(eventSystem) { moveDir = direction, moveVector = vector };
        ExecuteEvents.Execute(selected, data, ExecuteEvents.moveHandler);
    }

    // Virtual arrow keys. The phone sends an arrow event when its button goes down and keeps sending
    // it while the button is held. A key counts as held while events keep arriving, so a lost
    // "release" packet can never leave it stuck.
    private const float ArrowHoldWindow = 0.25f;   // a key stays "held" this long after the last event
    private const float NavRepeatDelay = 0.5f;     // menu navigation repeats like a held key: after this delay...
    private const float NavRepeatInterval = 0.1f;  // ...at this rate
    private static readonly float[] ArrowLastEvent = { -100f, -100f, -100f, -100f };
    private static readonly float[] ArrowNextRepeat = new float[4];

    /// <summary>
    /// An arrow key event from software. With a menu element selected it navigates the menu (repeating
    /// while held, like the keyboard); otherwise <see cref="IsArrowHeld"/> reports the key as held,
    /// which is what moves the camera while the menu is closed.
    /// </summary>
    public static void PressArrow(MoveDirection direction)
    {
        int i = (int)direction;
        if (i < 0 || i >= ArrowLastEvent.Length) return;

        float now = Time.unscaledTime;
        float sinceLast = now - ArrowLastEvent[i];
        bool newPress = sinceLast < 0f || sinceLast >= ArrowHoldWindow;
        ArrowLastEvent[i] = now;

        if (newPress)
        {
            ArrowNextRepeat[i] = now + NavRepeatDelay;
            Navigate(direction);
        }
        else if (now >= ArrowNextRepeat[i])
        {
            ArrowNextRepeat[i] = now + NavRepeatInterval;
            Navigate(direction);
        }
    }

    /// <summary>True while a software arrow key (see <see cref="PressArrow"/>) is held. Use next to Input.GetKey.</summary>
    public static bool IsArrowHeld(MoveDirection direction)
    {
        int i = (int)direction;
        if (i < 0 || i >= ArrowLastEvent.Length) return false;

        float since = Time.unscaledTime - ArrowLastEvent[i];
        return since >= 0f && since < ArrowHoldWindow;
    }

    /// <summary>Same as pressing Enter: activates the currently selected UI element.</summary>
    public static void Submit()
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        if (selected == null) return;

        ExecuteEvents.Execute(selected, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
    }
}