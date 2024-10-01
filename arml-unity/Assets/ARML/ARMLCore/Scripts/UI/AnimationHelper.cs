using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class AnimationHelper
{
    public static IEnumerator ZoomIn(RectTransform transform, float speed, UnityEvent OnEnd)
    {
        float time = 0;
        while (time < 1)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time);
            yield return null;
            time += Time.deltaTime * speed;
        }

        transform.localScale = Vector3.one;

        OnEnd?.Invoke();
    }

    public static IEnumerator ZoomOut(RectTransform transform, float speed, UnityEvent OnEnd)
    {
        float time = 0;
        while (time < 1)
        {
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, time);
            yield return null;
            time += Time.deltaTime * speed;
        }

        transform.localScale = Vector3.zero;
        OnEnd?.Invoke();
    }

    public static IEnumerator FadeIn(CanvasGroup canvasGroup, float speed, UnityEvent OnEnd)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float time = 0;
        while (time < 1)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, time);
            yield return null;
            time += Time.deltaTime * speed;
        }

        canvasGroup.alpha = 1;
        OnEnd?.Invoke();
    }

    public static IEnumerator FadeOut(CanvasGroup canvasGroup, float speed, UnityEvent OnEnd)
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        float time = 0;
        while (time < 1)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, time);
            yield return null;
            time += Time.deltaTime * speed;
        }

        canvasGroup.alpha = 0;
        OnEnd?.Invoke();
    }

    public static IEnumerator SlideIn(RectTransform transform, Page.EntryDirection direction, float speed, UnityEvent OnEnd)
    {
        Vector2 startPosition;
        switch (direction)
        {
            case Page.EntryDirection.Up:
                startPosition = new Vector2(0, -Screen.height);
                break;
            case Page.EntryDirection.Right:
                startPosition = new Vector2(-Screen.width, 0);
                break;
            case Page.EntryDirection.Down:
                startPosition = new Vector2(0, Screen.height);
                break;
            case Page.EntryDirection.Left:
                startPosition = new Vector2(Screen.width, 0);
                break;
            default:
                startPosition = new Vector2(0, -Screen.height);
                break;
        }

        float time = 0;
        while(time < 1)
        {
            transform.anchoredPosition = Vector2.Lerp(startPosition, Vector2.zero, time);
            yield return null;
            time += Time.deltaTime * speed;
        }

        transform.anchoredPosition = Vector2.zero;
        OnEnd?.Invoke();
    }

    public static IEnumerator SlideOut(RectTransform transform, Page.EntryDirection direction, float speed, UnityEvent OnEnd)
    {
        Vector2 endPosition;
        switch (direction)
        {
            case Page.EntryDirection.Up:
                endPosition = new Vector2(0, Screen.height);
                break;
            case Page.EntryDirection.Right:
                endPosition = new Vector2(Screen.width, 0);
                break;
            case Page.EntryDirection.Down:
                endPosition = new Vector2(0, -Screen.height);
                break;
            case Page.EntryDirection.Left:
                endPosition = new Vector2(-Screen.width, 0);
                break;
            default:
                endPosition = new Vector2(0, Screen.height);
                break;
        }

        float time = 0;
        while (time < 1)
        {
            transform.anchoredPosition = Vector2.Lerp(Vector2.zero, endPosition, time);
            yield return null;
            time += Time.deltaTime * speed;
        }

        transform.anchoredPosition = endPosition;
        OnEnd?.Invoke();
    }
}
