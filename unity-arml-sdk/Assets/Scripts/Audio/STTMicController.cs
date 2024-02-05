using DS;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the speech-to-text (STT) microphone functionality, managing voice command recognition and processing, as well as character conversation.
/// </summary>
public class STTMicController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject recordingIcon; // UI element to indicate recording status

    [HideInInspector] public bool isAwaitingInteractionCommand; // Flag to check if the system is awaiting a voice command
    [SerializeField] bool voiceCommandMode; // Toggle for enabling voice command mode
    [SerializeField] AudioClip micOnSFX; // Sound effect for microphone activation

    private Coroutine runningToggleCoroutine; // Coroutine for managing microphone toggling

    private bool isRecording = false; // Flag to track recording status

    private bool isDictation; // Flag to determine if the mode is dictation
    private VoskSpeechToText voskSTT; // Reference to the Vosk speech-to-text component
    private DSDialogue dsDialogue; // Reference to the dialogue system

    public static Action<string> OnVoiceCommandAction; // Action to trigger on receiving a voice command

    private float buttonPressTimer; // Timer to measure button press duration
    private const float requiredHoldTime = 0.33f; // Required time to hold button for voice command

    private AudioSource source; // Audio source for playing sounds

    /// <summary>
    /// Initializes the controller, setting up references and verifying components.
    /// </summary>
    void Awake()
    {
        voskSTT = FindObjectOfType<VoskSpeechToText>();
        if (voskSTT == null)
            Debug.LogError("VoskSpeechToText Instance not found in scene, make sure to add it if you are using STT");

        isDictation = voskSTT.processingMode == VoskSpeechToText.VoskProcessingMode.DICTATION ? true : false;
        voskSTT.OnTranscriptionResult += OnTranscriptionResult;

        source = GetComponent<AudioSource>();
        source.clip = micOnSFX;
    }

    private void Start()
    {
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
        if (!voiceCommandMode) return;

        if (Input.GetMouseButtonDown(0))
        {
            isAwaitingInteractionCommand = false;
            buttonPressTimer = 0;
        }

        if (Input.GetMouseButton(0))
        {
            if (isAwaitingInteractionCommand)
                return;

            if (buttonPressTimer < requiredHoldTime)
            {
                buttonPressTimer += Time.deltaTime;
            }
            else
            {
                print("Started listening for voice commands");
                isAwaitingInteractionCommand = true;
                ForceRecordingOn();
                source.Play();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!isAwaitingInteractionCommand) return;

            print("Stopped listening for voice commands");
            if (voskSTT.processingMode == VoskSpeechToText.VoskProcessingMode.STANDARD)
                isAwaitingInteractionCommand = true;

            //Delayed to avoid cutting off processing in the middle of the last word
            StartCoroutine(DelayedForceRecordingOff(0.5f));
            buttonPressTimer = 0;
        }
    }

    /// <summary>
    /// Delays turning off the recording for a specified duration.
    /// </summary>
    /// <param name="delayInSeconds">The delay in seconds before turning off the recording.</param>
    /// <returns>An IEnumerator for the coroutine.</returns>
    private IEnumerator DelayedForceRecordingOff(float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);
        ForceRecordingOff();
    }


    /// <summary>
    /// Processes the transcription result, executing relevant actions based on the recognized text.
    /// </summary>
    /// <param name="obj">The transcribed text.</param>
    private void OnTranscriptionResult(string obj)
    {
        var result = new RecognitionResult(obj);

        print("STT Result: " + result.Phrases[0].Text);

        if (isAwaitingInteractionCommand)
        {
            //Do voice command stuff
            ProcessVoiceCommands(result.Phrases[0].Text);
            if (voskSTT.processingMode == VoskSpeechToText.VoskProcessingMode.STANDARD)
                isAwaitingInteractionCommand = false;
        }

        if (dsDialogue == null)
        {
            Debug.LogWarning("STTMicController trying to access non-referenced DialogueSystem, make sure it's set correctly");
            return;
        }
        else
        {
            dsDialogue.CheckTranscriptionResult(result.Phrases[0].Text, isDictation);
        }
    }

    /// <summary>
    /// Processes voice commands based on the recognized text.
    /// </summary>
    /// <param name="result">The recognized voice command text.</param>
    private void ProcessVoiceCommands(string result)
    {
        //Nothing was received, ignore
        if (string.IsNullOrEmpty(result))
        {
            return;
        }

        OnVoiceCommandAction?.Invoke(result);

        //WORK IN PROGRESS

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
        isDictation = voskSTT.processingMode == VoskSpeechToText.VoskProcessingMode.DICTATION ? true : false;
    }

    /// <summary>
    /// Toggles the recording state, with an option for auto-stop after a specified duration.
    /// </summary>
    /// <param name="secondsToAutoStop">Duration in seconds after which the recording should automatically stop.</param>
    public void ToggleRecording(float secondsToAutoStop = 0f)
    {
        runningToggleCoroutine = StartCoroutine(ToggleRecordingCoroutine(secondsToAutoStop));
    }

    /// <summary>
    /// Forces the recording to start, regardless of the current recording state.
    /// </summary>
    public void ForceRecordingOn()
    {
        if (isRecording)
            return;

        isRecording = true;
        recordingIcon.SetActive(isRecording);

        if (runningToggleCoroutine != null)
            StopCoroutine(runningToggleCoroutine);

        voskSTT?.ToggleRecording();
        //voskSTT.shouldContinueProcessing = isRecording;
    }

    /// <summary>
    /// Forces the recording to stop, regardless of the current recording state.
    /// </summary>
    public void ForceRecordingOff()
    {
        if (!isRecording)
            return;

        isRecording = false;
        recordingIcon.SetActive(isRecording);
        voskSTT?.ToggleRecording();

        if (runningToggleCoroutine != null)
            StopCoroutine(runningToggleCoroutine);

        //voskSTT.shouldContinueProcessing = isRecording;
    }

    /// <summary>
    /// Coroutine to toggle recording state, with an optional auto-stop feature.
    /// </summary>
    /// <param name="secondsToAutoStop">Duration in seconds after which the recording should automatically stop.</param>
    /// <returns>Returns an IEnumerator for coroutine functionality.</returns>
    public IEnumerator ToggleRecordingCoroutine(float secondsToAutoStop = 0f)
    {
        //Change bool
        isRecording = !isRecording;

        //Workaround for dictation mode
        if (!isRecording)
            isDictation = false;

        recordingIcon.SetActive(isRecording);
        voskSTT?.ToggleRecording();
        //voskSTT.shouldContinueProcessing = isRecording;

        //Auto stop recording after given seconds
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
}
