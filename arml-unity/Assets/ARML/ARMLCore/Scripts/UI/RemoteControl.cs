using System;
using System.Collections;
using UnityEngine;

public class RemoteControl : MonoBehaviour {
    public static RemoteControl Instance;

    [Tooltip("Time in seconds for long press to activate")]
    public float LongPressTime = 3;

    [Tooltip("Menu key assignment")]
    public KeyCode MenuKey = KeyCode.Menu;

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
        if (Instance == null) {
            Instance = this;
        }
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
    }
}