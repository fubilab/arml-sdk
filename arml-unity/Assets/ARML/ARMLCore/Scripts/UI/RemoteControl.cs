using System;
using System.Collections;
using UnityEngine;

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
    [Tooltip("Reset pose key assignment (for systems that do not support long press).")]
    public KeyCode ResetKey = KeyCode.Backspace;

    public Action OnMenuPress;
    public Action OnMenuLongPress;

    IEnumerator _MenuButtonPressCoroutine = null;

    IEnumerator MenuButtonPress() {
        yield return new WaitForSeconds(LongPressTime);
        OnMenuLongPress?.Invoke();
        // print("Menu long press");
        _MenuButtonPressCoroutine = null;

    }

    void Awake() {
        Singleton();
    }


    private void Update()
    {
        if (Input.GetKeyDown(MenuKey))
        {
            //print("Down: Menu");
            _MenuButtonPressCoroutine = MenuButtonPress();
            StartCoroutine(_MenuButtonPressCoroutine);
        }
        if (Input.GetKeyUp(MenuKey))
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
}