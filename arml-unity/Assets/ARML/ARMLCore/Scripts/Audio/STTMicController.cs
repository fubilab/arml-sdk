using DS;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARML
{
    /// <summary>
    /// Controls the speech-to-text (STT) microphone functionality, managing voice command recognition and processing, as well as character conversation.
    /// </summary>
    public class STTMicController : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("UI element to indicate recording status.")]
        [SerializeField] GameObject recordingIcon;

        [SerializeField] Image micImage;

        [HideInInspector] public bool currentlyRecording; // Flag to check if the system is awaiting a voice command

        [Tooltip("Toggle for enabling voice command mode.")]
        public bool voiceCommandMode;

        [Tooltip("Sound effect for microphone activation.")]
        [SerializeField] AudioClip micOnSFX;

        [Tooltip("Color used for the recording indicator.")]
        [SerializeField] Color recordingColour;

        private Coroutine runningToggleCoroutine; // Coroutine for managing microphone toggling
        private Coroutine cooldownCoroutine; // Added for cooldown

        private bool isRecording = false; // Flag to track recording status

        private bool isDictation; // Flag to determine if the mode is dictation
        private VoskSpeechToText voskSTT; // Reference to the Vosk speech-to-text component
        private DSDialogue dsDialogue; // Reference to the dialogue system

        public static Action<string> OnVoiceCommandAction; // Action to trigger on receiving a voice command

        private float buttonPressTimer; // Timer to measure button press duration
        private const float requiredHoldTime = 0.33f; // Required time to hold button for voice command

        private AudioSource source; // Audio source for playing sounds

        [HideInInspector] public bool fromDwellToRecord;

        [SerializeField] CrosshairController crosshairController;

        private bool cooldownActive = false; // Added for cooldown


        /// <summary>
        /// Subscribe to events when this component is enabled.
        /// </summary>
        private void OnEnable()
        {
            DSDialogue.OnStartRecordingAttemptAction += () => voiceCommandMode = true;
            DSDialogue.OnStopRecordingAttemptAction += () =>
            {
                if (!currentlyRecording)
                    voiceCommandMode = false;
            };
        }

        /// <summary>
        /// Unsubscribe from events when this component is disabled.
        /// </summary>
        private void OnDisable()
        {
            DSDialogue.OnStartRecordingAttemptAction -= () => voiceCommandMode = true;
            DSDialogue.OnStopRecordingAttemptAction -= () =>
            {
                if (!currentlyRecording)
                    voiceCommandMode = false;
            };
        }

        /// <summary>
        /// Initializes the controller, setting up references and verifying components.
        /// </summary>
        void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the controller, setting up references and verifying components.
        /// </summary>
        private void Initialize()
        {
            voskSTT = FindObjectOfType<VoskSpeechToText>();
            if (voskSTT == null)
                Debug.LogError("VoskSpeechToText Instance not found in scene, make sure to add it if you are using STT");

            isDictation = voskSTT.processingMode == VoskSpeechToText.VoskProcessingMode.DICTATION;
            voskSTT.OnTranscriptionResult += OnTranscriptionResult;

            source = GetComponent<AudioSource>();
            source.clip = micOnSFX;

            recordingIcon.SetActive(false);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Continuously handles voice command input.
        /// </summary>
        private void Update()
        {
            HandleVoiceCommandInput();
        }

        /// <summary>
        /// Handles the voice command input, starting and stopping the recording based on user interaction.
        /// </summary>
        private void HandleVoiceCommandInput()
        {
            if (!voiceCommandMode || cooldownActive) return;

            if (Input.GetMouseButtonDown(0))
            {
                currentlyRecording = false;
                buttonPressTimer = 0;
            }

            if (Input.GetMouseButton(0))
            {
                if (currentlyRecording)
                    return;

                if (buttonPressTimer < requiredHoldTime)
                {
                    buttonPressTimer += Time.deltaTime;
                }
                else
                {
                    StartRecording();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (!currentlyRecording) return;

                StopRecording();

                // Start cooldown coroutine
                StartCoroutine(StartCooldown());
            }
        }

        /// <summary>
        /// Starts the recording process.
        /// </summary>
        private void StartRecording()
        {
            print("Started recording");
            currentlyRecording = true;
            ForceRecordingOn();
            source.Play();
        }

        /// <summary>
        /// Stops the recording process.
        /// </summary>
        private void StopRecording()
        {
            print("Stopped recording");
            if (voskSTT.processingMode == VoskSpeechToText.VoskProcessingMode.STANDARD)
                currentlyRecording = true;

            CrosshairController.Instance.ChangeCrosshairState(CrosshairController.CrosshairState.IDLE);
            StartCoroutine(DelayedForceRecordingOff(0.5f));
            buttonPressTimer = 0;
        }

        /// <summary>
        /// Delays turning off the recording for a specified duration.
        /// </summary>
        /// <param name="delayInSeconds">The delay in seconds before turning off the recording.</param>
        private IEnumerator DelayedForceRecordingOff(float delayInSeconds)
        {
            yield return new WaitForSeconds(delayInSeconds);
            ForceRecordingOff();
        }

        /// <summary>
        /// Processes the transcription result, executing relevant actions based on the recognized text.
        /// </summary>
        /// <param name="transcription">The transcribed text.</param>
        private void OnTranscriptionResult(string transcription)
        {
            print("STT Result: " + transcription);

            if (currentlyRecording)
            {
                ProcessVoiceCommands(transcription);
                if (voskSTT.processingMode == VoskSpeechToText.VoskProcessingMode.STANDARD)
                {
                    currentlyRecording = false;
                    if (crosshairController.previousState != CrosshairController.CrosshairState.CANRECORD)
                        crosshairController.ChangeToPreviousState();
                }
            }

            if (dsDialogue == null)
            {
                Debug.LogWarning("STTMicController trying to access non-referenced DialogueSystem, make sure it's set correctly");
                return;
            }
            else
            {
                dsDialogue.CheckTranscriptionResult(transcription, isDictation);
            }
        }

        /// <summary>
        /// Processes voice commands based on the recognized text.
        /// </summary>
        /// <param name="result">The recognized voice command text.</param>
        private void ProcessVoiceCommands(string result)
        {
            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            OnVoiceCommandAction?.Invoke(result);

            //TODO - WORK IN PROGRESS

            List<int> possibleMatchesIndexes = new List<int>();

            //Loop through all choice texts to find matches
            //for (int i = 0; i < dialogue.Choices.Count; i++)
            //{
            //    //Remove accents!
            //    if (RemoveDiacritics(result).Contains(RemoveDiacritics(dialogue.Choices[i].Text), StringComparison.InvariantCultureIgnoreCase))
            //    {
            //        //Success! Add to match list
            //        possibleMatchesIndexes.Add(i);
            //    }
            //}

            ////If more than one match, or none, ask again
            //if (possibleMatchesIndexes.Count != 1 && !isDictation)
            //{
            //    //Ask again, say the answer was not clear
            //    StartCoroutine(DisplayDefaultAnswer(1));
            //    sttMicController.ResetDictationMode();
            //    return;
            //}
            //else if (possibleMatchesIndexes.Count == 1) //If one match exactly, answer accepted, immediately even if dictation
            //{
            //    GoToNextDialogue(possibleMatchesIndexes[0]);
            //    sttMicController.ForceRecordingOff();
            //}
        }

        /// <summary>
        /// Resets the dictation mode based on the current processing mode of VoskSpeechToText.
        /// </summary>
        public void ResetDictationMode()
        {
            isDictation = voskSTT.processingMode == VoskSpeechToText.VoskProcessingMode.DICTATION;
        }

        /// <summary>
        /// Forces the recording to stop, regardless of the current recording state.
        /// </summary>
        public void ForceRecordingOff()
        {
            if (!isRecording)
                return;

            isRecording = false;
            micImage.color = Color.white;
            voskSTT?.ToggleRecording();

            if (runningToggleCoroutine != null)
                StopCoroutine(runningToggleCoroutine);
        }

        /// <summary>
        /// Forces the recording to start, regardless of the current recording state.
        /// </summary>
        private void ForceRecordingOn()
        {
            if (isRecording)
                return;

            isRecording = true;
            micImage.color = recordingColour;

            if (runningToggleCoroutine != null)
                StopCoroutine(runningToggleCoroutine);

            voskSTT?.ToggleRecording();
        }

        /// <summary>
        /// Coroutine to toggle recording state, with an optional auto-stop feature.
        /// </summary>
        /// <param name="secondsToAutoStop">Duration in seconds after which the recording should automatically stop.</param>
        /// <returns>Returns an IEnumerator for coroutine functionality.</returns>
        public IEnumerator ToggleRecordingCoroutine(float secondsToAutoStop = 0f)
        {
            isRecording = !isRecording;

            if (isRecording)
                currentlyRecording = true;

            source.Play();

            if (!isRecording)
                isDictation = false;

            micImage.color = isRecording ? recordingColour : Color.white;

            voskSTT?.ToggleRecording();

            if (secondsToAutoStop == 0)
            {
                yield break;
            }
            else
            {
                yield return new WaitForSeconds(secondsToAutoStop);
                StartCoroutine(ToggleRecordingCoroutine(0));
            }
        }

        /// <summary>
        /// Sets the current character dialogue system for processing the STT results.
        /// </summary>
        /// <param name="ds">The DSDialogue instance to be used.</param>
        public void SetCurrentDialogueSystem(DSDialogue ds)
        {
            dsDialogue = ds;
        }

        private IEnumerator StartCooldown()
        {
            cooldownActive = true;
            yield return new WaitForSeconds(3f); // Adjust cooldown duration as needed
            cooldownActive = false;
        }
    }
}