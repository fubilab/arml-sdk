using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Added to a phone-side UI button to act like a held key: <see cref="OnPress"/> fires the moment the
/// button goes down, then <see cref="OnRepeat"/> fires repeatedly for as long as it stays held.
/// It only listens for press/release, so dragging the scroll view from the button still works.
/// (Use it instead of the button's click: a click only fires on release.)
/// </summary>
public class OSCHoldRepeat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Action OnPress;
    public Action OnRepeat;
    public float Delay = 0.1f;     // seconds from press to the first repeat
    public float Interval = 0.1f;  // seconds between repeats

    private bool _held;
    private float _next;

    public void OnPointerDown(PointerEventData eventData)
    {
        _held = true;
        _next = Time.unscaledTime + Delay;
        OnPress?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData) => _held = false;
    public void OnPointerExit(PointerEventData eventData) => _held = false;
    private void OnDisable() => _held = false;

    private void Update()
    {
        if (!_held || Time.unscaledTime < _next) return;

        _next = Time.unscaledTime + Interval;
        OnRepeat?.Invoke();
    }
}
