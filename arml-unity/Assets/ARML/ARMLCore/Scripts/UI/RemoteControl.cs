using System;
using System.Collections;
using UnityEngine;

public class RemoteControl : MonoBehaviour {
    public static RemoteControl Instance;

    public Action OnMenuPress;
    public Action OnMenuLongPress;

    IEnumerator _MenuButtonPressCoroutine = null;

    IEnumerator MenuButtonPress() {
        yield return new WaitForSeconds(3);
        print("Menu long press");
        _MenuButtonPressCoroutine = null;

    }

    void Awake() {
        if (Instance == null) {
            Instance = this;
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Menu))
        {
            print("Down: Menu");
            
            _MenuButtonPressCoroutine = MenuButtonPress();
            StartCoroutine(_MenuButtonPressCoroutine);
        }
        if (Input.GetKeyUp(KeyCode.Menu))
        {
            if (_MenuButtonPressCoroutine != null) {
                StopCoroutine(_MenuButtonPressCoroutine);
                _MenuButtonPressCoroutine = null;
                print("Up: Menu");
                if (OnMenuPress != null) {
                    OnMenuPress.Invoke();
                }
            }
        }
    }
}