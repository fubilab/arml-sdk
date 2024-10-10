using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace ARML.Interaction
{
    /// <summary>
    /// Provides audio and visual feedback for actions. This includes playing sound effects and particle systems.
    /// </summary>
    public class ActionFeedback : MonoBehaviour
    {
        [Header("Hover SFX")]
        [SerializeField] AudioClip hoverSFX;
        [SerializeField] float hoverSFXDuration = 1;

        [Header("Trigger SFX")]
        [SerializeField] List<AudioClip> triggerSFXClips = new List<AudioClip>();
        [SerializeField] float triggerSFXDelay;

        [Header("Progress SFX")]
        [SerializeField] AudioClip progressSFXClip;

        [Header("AudioSource")]
        [SerializeField] AudioSource audioSource;

        [Header("Volumes")]
        [SerializeField][Range(0, 1)] float triggerVolume = 1.0f;
        [SerializeField][Range(0, 1)] float progressVolume = 0.5f;
        [SerializeField] float progressFadeAmount = 0.5f;
        [SerializeField] Vector2 randomPitchRange = new Vector2(1, 1);

        [Header("Particles")]
        [SerializeField] ParticleSystem particleSystem;
        [SerializeField] float particlePlayDelay = 0f;
        [SerializeField] AudioClip particleSFX;

        private int currentTriggerSFXIndex;

        private Coroutine hoverCoroutine;
        private Coroutine triggerCoroutine;

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

                if (triggerCoroutine == null)
                    triggerCoroutine = StartCoroutine(PlaySFX(triggerSFXDelay));
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

                StartCoroutine(PlaySFX(0));
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
                audioSource.DOFade(0f, 0.2f);
                // audioSource.Stop(); // No need to stop because now it does not loop, let it fade and then stops automatically
            }
        }

        /// <summary>
        /// Plays the sound effect with a random pitch.
        /// </summary>
        private IEnumerator PlaySFX(float delayInSeconds)
        {
            audioSource.pitch = (Random.Range(randomPitchRange.x, randomPitchRange.y));
            yield return new WaitForSeconds(delayInSeconds);
            audioSource.Play();
            yield return new WaitForSeconds(0.5f);
            triggerCoroutine = null;
        }

        /// <summary>
        /// Coroutine to play particle effects with a delay.
        /// </summary>
        /// <returns>IEnumerator for coroutine functionality.</returns>
        public IEnumerator PlayParticlesCoroutine()
        {
            if (particleSFX != null)
                AudioSource.PlayClipAtPoint(particleSFX, transform.position);
            yield return new WaitForSeconds(particlePlayDelay);
            particleSystem.Play();
        }

        public void PlayHoverSFX()
        {
            if (hoverCoroutine == null)
                hoverCoroutine = StartCoroutine(PlayHoverSFXCoroutine());
        }

        private IEnumerator PlayHoverSFXCoroutine()
        {
            audioSource.clip = hoverSFX;
            audioSource.loop = true;
            audioSource.volume = 1f;
            audioSource.Play();
            yield return new WaitForSeconds(hoverSFXDuration);
            audioSource.DOFade(0f, 0.5f);
            yield return new WaitForSeconds(0.5f);
            audioSource.Stop();
            audioSource.loop = false;
            hoverCoroutine = null;
        }
    }
}