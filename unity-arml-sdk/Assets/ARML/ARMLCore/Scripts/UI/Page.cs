using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

[RequireComponent(typeof(AudioSource), typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class Page : MonoBehaviour
{
    public enum EntryMode
    {
        DoNothing,
        Slide,
        Zoom,
        Fade
    }

    public enum EntryDirection
    {
        None,
        Up,
        Right,
        Down,
        Left
    }

    private AudioSource audioSource;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    [SerializeField]
    private float animationSpeed = 4f;
    public bool exitOnNewPagePush = false;
    [SerializeField]
    private AudioClip entryClip;
    [SerializeField]
    private AudioClip exitClip;
    [SerializeField]
    EntryMode entryMode = EntryMode.Slide;
    [SerializeField]
    EntryDirection entryDirection = EntryDirection.Left;
    [SerializeField]
    EntryMode exitMode = EntryMode.Slide;
    [SerializeField]
    EntryDirection exitDirection = EntryDirection.Left;
    [SerializeField]
    private UnityEvent PrePushAction;
    [SerializeField]
    private UnityEvent PostPushAction;
    [SerializeField]
    private UnityEvent PrePopAction;
    [SerializeField]
    private UnityEvent PostPopAction;

    private Coroutine animationCoroutine;
    private Coroutine audioCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0;
        audioSource.enabled = false;
    }

    public void Enter(bool playAudio)
    {
        PrePushAction?.Invoke();

        switch (entryMode)
        {
            case EntryMode.Slide:
                SlideIn(playAudio);
                break;
            case EntryMode.Zoom:
                ZoomIn(playAudio);
                break;
            case EntryMode.Fade:
                FadeIn(playAudio);
                break;
        }
    }

    public void Exit(bool playAudio)
    {
        PrePopAction?.Invoke();

        switch (exitMode)
        {
            case EntryMode.Slide:
                SlideOut(playAudio);
                break;
            case EntryMode.Zoom:
                ZoomOut(playAudio);
                break;
            case EntryMode.Fade:
                FadeOut(playAudio);
                break;
        }
    }

    void SlideIn(bool playAudio)
    {
        if(animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimationHelper.SlideIn(rectTransform, entryDirection, animationSpeed, PostPushAction));

        PlayEntryClip(playAudio);
    }

    void SlideOut(bool playAudio)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimationHelper.SlideOut(rectTransform, exitDirection, animationSpeed, PostPopAction));

        PlayExitClip(playAudio);
    }

    void ZoomIn(bool playAudio)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimationHelper.ZoomIn(rectTransform, animationSpeed, PostPushAction));

        PlayEntryClip(playAudio);
    }

    void ZoomOut(bool playAudio)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimationHelper.ZoomOut(rectTransform, animationSpeed, PostPopAction));

        PlayExitClip(playAudio);
    }

    void FadeIn(bool playAudio)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimationHelper.FadeIn(canvasGroup, animationSpeed, PostPushAction));

        PlayEntryClip(playAudio);
    }

    void FadeOut(bool playAudio)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimationHelper.FadeOut(canvasGroup, animationSpeed, PostPopAction));

        PlayExitClip(playAudio);
    }

    void PlayEntryClip(bool playAudio)
    {
        if(playAudio && entryClip != null && audioSource != null)
        {
            if(audioCoroutine != null)
            {
                StopCoroutine(audioCoroutine);
            }

            audioCoroutine = StartCoroutine(PlayClip(entryClip));
        }
    }

    void PlayExitClip(bool playAudio)
    {
        if (playAudio && exitClip != null && audioSource != null)
        {
            if (audioCoroutine != null)
            {
                StopCoroutine(audioCoroutine);
            }

            audioCoroutine = StartCoroutine(PlayClip(exitClip));
        }
    }

    //Play clip once, then after length of clip disable audio source
    IEnumerator PlayClip(AudioClip clip)
    {
        audioSource.enabled = true;
        WaitForSeconds wait = new WaitForSeconds(clip.length);
        audioSource.PlayOneShot(clip);
        yield return wait;
        audioSource.enabled = false;
    }
}
