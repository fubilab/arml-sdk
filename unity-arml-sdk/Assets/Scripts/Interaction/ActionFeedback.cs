using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Provides audio and visual feedback for actions. This includes playing sound effects and particle systems.
/// </summary>
public class ActionFeedback : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] List<AudioClip> triggerSFXClips = new List<AudioClip>();
    [SerializeField] AudioClip progressSFXClip;
    [SerializeField] AudioSource audioSource;

    [Header("Volumes")]
    [SerializeField][Range(0, 1)] float triggerVolume = 1.0f;
    [SerializeField][Range(0, 1)] float progressVolume = 0.5f;
    [SerializeField] float progressFadeAmount = 0.5f;
    [SerializeField] Vector2 randomPitchRange = new Vector2(1, 1);

    [Header("Particles")]
    [SerializeField] ParticleSystem particleSystem;
    [SerializeField] float particlePlayDelay = 0f;

    private int currentTriggerSFXIndex;

    /// <summary>
    /// Initializes the component, setting up audio and particle system references.
    /// </summary>
    private void Start()
    {
        if (audioSource == null && GetComponent<AudioSource>())
            audioSource = GetComponent<AudioSource>();
        if (particleSystem == null && GetComponentInChildren<ParticleSystem>())
            particleSystem = GetComponentInChildren<ParticleSystem>();

        if (triggerSFXClips.Count <= 0)
            return;

        currentTriggerSFXIndex = Random.Range(0, triggerSFXClips.Count);
        audioSource.clip = triggerSFXClips[currentTriggerSFXIndex];
    }

    /// <summary>
    /// Plays a random trigger sound effect and triggers the particle system.
    /// </summary>
    public void PlayRandomTriggerFeedback()
    {
        if (triggerSFXClips.Count > 0)
        {
            currentTriggerSFXIndex = Random.Range(0, triggerSFXClips.Count);
            audioSource.clip = triggerSFXClips[currentTriggerSFXIndex];
            audioSource.loop = false;
            audioSource.volume = triggerVolume;
            PlaySFX();
        }

        if (particleSystem)
            StartCoroutine(PlayParticlesCoroutine());
    }

    /// <summary>
    /// Plays a looping progress sound effect with a fade-in.
    /// </summary>
    public void PlayProgressFeedback()
    {
        // No matter what if there is a sfx currently playing, wait
        if (audioSource.isPlaying)
            return;

        // If there is a progress sfx and it is not currently playing, play it
        if (progressSFXClip)
        {
            audioSource.clip = progressSFXClip;
            audioSource.loop = true;

            // Fade volume with DOTween
            audioSource.volume = 0f;
            audioSource.DOFade(progressVolume, progressFadeAmount);

            PlaySFX();
        }
    }

    /// <summary>
    /// Stops the progress sound effect with a fade-out.
    /// </summary>
    public void StopProgressFeedback()
    {
        if (!audioSource.isPlaying)
            return;

        if (progressSFXClip)
        {
            audioSource.loop = false;
            audioSource.DOFade(0f, 0.1f);
            // audioSource.Stop(); // No need to stop because now it does not loop, let it fade and then stops automatically
        }
    }

    /// <summary>
    /// Plays the sound effect with a random pitch.
    /// </summary>
    private void PlaySFX()
    {
        audioSource.pitch = (Random.Range(randomPitchRange.x, randomPitchRange.y));
        audioSource.Play();
    }

    /// <summary>
    /// Coroutine to play particle effects with a delay.
    /// </summary>
    /// <returns>IEnumerator for coroutine functionality.</returns>
    public IEnumerator PlayParticlesCoroutine()
    {
        yield return new WaitForSeconds(particlePlayDelay);
        particleSystem.Play();
    }
}
