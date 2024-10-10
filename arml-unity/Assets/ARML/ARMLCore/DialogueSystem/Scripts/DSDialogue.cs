using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using ARML.Language;
using ARML.Voice;
using ARML.Interaction;
using ARML.DebugTools;
using ARML.SceneManagement;
using ARML.UI;

namespace DS
{
    using Enumerations;
    using ScriptableObjects;
    using System.Linq;

    /// <summary>
    /// Manages dialogue interactions with a character, handling speech-to-text, choices, and events.
    /// Supports multiple dialogue types and integrates audio for responses and default answers.
    /// </summary>
    public class DSDialogue : MonoBehaviour
    {
        /* Dialogue Scriptable Objects */
        [SerializeField] private DSDialogueContainerSO dialogueContainer;
        [SerializeField] private DSDialogueGroupSO dialogueGroup;
        [SerializeField] private DSDialogueSO dialogue;
        [SerializeField] private DSDialogueSO startingDialogue;

        /* Filters */
        [SerializeField] private bool groupedDialogues;
        [SerializeField] private bool startingDialoguesOnly;

        /* Indexes */
        [SerializeField] private int selectedDialogueGroupIndex;
        [SerializeField] private int selectedDialogueIndex;

        // Dialogue Text
        [SerializeField] private TMP_Text dialogueDisplayText;
        [SerializeField] private List<TMP_Text> choiceDisplayTexts;

        // Behaviour
        public enum DialogueInteractionType
        {
            Selection,
            Speaking
        }

        [SerializeField] private DialogueInteractionType dialogueInteractionType = DialogueInteractionType.Selection;

        [Tooltip("Automatically advances the dialogue for single-choice dialogues")]
        [SerializeField] private bool autoContinueSingleChoice;
        [SerializeField] private float secondsToAutoContinue;
        [SerializeField] private bool autoStartDialogue;

        // Default Answers
        [SerializeField] private AudioClip didNotHearClipEN;
        [SerializeField] private AudioClip didNotHearClipES;
        [SerializeField] private AudioClip didNotUnderstandClipEN;
        [SerializeField] private AudioClip didNotUnderstandClipES;

        //STT Vosk
        private STTMicController sttMicController;

        // UnityEvent
        [SerializeField] private UnityEvent OnDialogueEventIndex1;
        [SerializeField] private UnityEvent OnDialogueEventIndex2;
        [SerializeField] private UnityEvent OnDialogueFinishedEvent;
        [SerializeField] private List<LevelEvent> OnInteractedEvent;

        private AudioSource audioSource;
        private VoskSpeechToText voskSTT;

        //Direct Talking Interaction
        private Collider interactionCollider;
        private bool canBeInteractedWith;
        private bool pendingTalkInteraction;
        private AudioClip audioWhenInteracted;
        private bool advanceLevelWhenInteracted;

        public static Action OnStartTalkingAttemptAction;
        public static Action OnStopTalkingAttemptAction;

        public static Action OnStartRecordingAttemptAction;
        public static Action OnStopRecordingAttemptAction;

        private bool dialogueStarted;

        private Coroutine playAudioCurrentDialogue;

        private bool interactionWithinSystem;

        private bool dontRequireDwellOnce;

        private CameraPointedObject camPointedObject;

        private bool playerLookingAtCharacter;

        private Coroutine playAudioOutsideOfSystemCoroutine;

        /// <summary>
        /// Initializes dialogue settings and prepares for interaction.
        /// </summary>
        private void Start()
        {
            audioSource = GetComponent<AudioSource?>();
            sttMicController = FindObjectOfType<STTMicController>(true);
            voskSTT = FindObjectOfType<VoskSpeechToText>();
            interactionCollider = GetComponent<Collider?>();
            camPointedObject = GetComponent<CameraPointedObject>();
            canBeInteractedWith = true;

            if (sttMicController == null)
                Debug.LogError("No STTMicController found in the scene, make sure to add one for speech-to-text");

            if (interactionCollider == null)
                Debug.LogError($"No Collider found for Dialogue Character {name}, make sure to add one if you want interaction");

            //Remove text from displayTexts
            if (dialogueDisplayText != null)
                dialogueDisplayText.text = string.Empty;
            foreach (TMP_Text choiceDisplayText in choiceDisplayTexts)
                choiceDisplayText.gameObject.SetActive(false);

            startingDialogue = dialogue;

            if (startingDialogue == null)
                return;

            //If Interaction Type is not Selection, deactivate choice camera pointed objects
            //if (dialogueInteractionType != DialogueInteractionType.Selection)
            //{
            //    foreach (var choiceText in choiceDisplayTexts)
            //    {
            //        choiceText.GetComponent<CameraPointedObject?>().enabled = false;
            //    }
            //}

            if (autoStartDialogue)
                RestartDialogue();
        }

        /// <summary>
        /// Handles key inputs for debugging or specific interactions.
        /// </summary>
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (camPointedObject?.interactionType == InteractionType.BUTTON)
                    CheckInteraction();
            }

            if (sttMicController != null)
            {
                if (sttMicController.currentlyRecording)
                {
                    StopCoroutine(playAudioCurrentDialogue);
                    audioSource.Stop();
                }
            }
        }

        public void CheckInteraction()
        {
            if (pendingTalkInteraction && canBeInteractedWith)
            {
                if (interactionWithinSystem)
                {
                    if (playAudioCurrentDialogue != null)
                        StopCoroutine(playAudioCurrentDialogue);
                    playAudioCurrentDialogue = StartCoroutine(PlayAudioCurrentDialogue());
                }
                else
                {
                    if (audioWhenInteracted != null)
                    {
                        if (playAudioOutsideOfSystemCoroutine != null)
                            StopCoroutine(playAudioOutsideOfSystemCoroutine);
                        playAudioOutsideOfSystemCoroutine = StartCoroutine(PlayAudioOutsideOfSystemCoroutine(audioWhenInteracted));
                    }
                }

                OnStopTalkingAttemptAction();
                pendingTalkInteraction = false;

                //State machine to allow answering after dwell-triggered question repeat
                if (dialogueInteractionType == DialogueInteractionType.Speaking)
                {
                    //sttMicController.currentlyRecording = true;
                    sttMicController.fromDwellToRecord = true;
                    OnStartRecordingAttemptAction();
                }

                int currentLevel = LevelController.Instance.currentLevel.levelIndex + 1;
                foreach (LevelEvent levelEvent in OnInteractedEvent)
                {
                    if (levelEvent.levelIndex == currentLevel)
                        levelEvent.levelEvent?.Invoke();
                }

                //Log to CSV Export
                MonitoredAction levelmonitoredAction = new MonitoredAction()
                {
                    ActionType = MonitoredAction.ActionTypeEnum.USED,
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ActionObject = this.gameObject.name
                };

                CSVExport.Instance?.monitoredActions.Add(levelmonitoredAction);
            }
        }

        public void SetInteractionAudioToSystem(bool state)
        {
            interactionWithinSystem = state;
        }

        /// <summary>
        /// Advances to the next dialogue based on the player's choice.
        /// </summary>
        /// <param name="choiceIndex">The index of the player's choice.</param>
        public void GoToNextDialogue(int choiceIndex)
        {
            DSDialogueSO nextDialogue = dialogue.Choices[choiceIndex].NextDialogue;
            if (nextDialogue != null)
            {
                if (dialogue.DialogueType == DSDialogueType.MultipleChoice)
                {
                    Debug.Log($"Dialogue Choice {dialogue.Choices[choiceIndex].TextEN} was selected");
                }

                //If placed here, it will happen right before next dialogue
                InvokeEventDialogue();

                dialogue = nextDialogue;
                DisplayTextCurrentDialogue();

                //If placed here, it will happen right when the dialogue appears
                //InvokeEventDialogue();

                //Stop then Start Audio
                if (playAudioCurrentDialogue != null)
                    StopCoroutine(playAudioCurrentDialogue);
                playAudioCurrentDialogue = StartCoroutine(PlayAudioCurrentDialogue());
            }
            //This way we make sure that "fake endings" in the dialogue don't trigger the end event unless they have 0 in the eventID
            else if (nextDialogue == null && dialogue.EventID == 0)
            {
                DialogueFinished(); //Can use this to trigger events - old version
                dialogueDisplayText.text = "";
            }
        }

        /// <summary>
        /// Invokes an event based on the current dialogue's event ID.
        /// </summary>
        private void InvokeEventDialogue()
        {
            //If Event ID is not 0, then an event is expected
            if (dialogue.EventID > 0)
            {
                int eventID = dialogue.EventID;
                switch (eventID)
                {
                    case 1:
                        OnDialogueEventIndex1?.Invoke();
                        break;
                    case 2:
                        OnDialogueEventIndex2?.Invoke();
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Plays the audio clip associated with the current dialogue.
        /// </summary>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator PlayAudioCurrentDialogue()
        {
            //If no AudioSource, can't play audio
            if (audioSource == null)
                yield break;

            //Check if current dialogue has audio for current language
            switch (LanguageController.Instance.currentLanguage)
            {
                case Languages.EN:
                    if (dialogue.AudioClipEN == null)
                        yield break;
                    else
                        audioSource.clip = dialogue.AudioClipEN;
                    break;
                case Languages.ES:
                    if (dialogue.AudioClipES == null)
                        yield break;
                    else
                        audioSource.clip = dialogue.AudioClipES;
                    break;
                case Languages.CA:
                    if (dialogue.AudioClipCA == null)
                        yield break;
                    else
                        audioSource.clip = dialogue.AudioClipCA;
                    break;
            }

            //Stop current clip if there was one
            audioSource.Stop();
            audioSource.Play();

            if (dialogueInteractionType == DialogueInteractionType.Speaking)
            {
                if (dialogue.DialogueType == DSDialogueType.MultipleChoice)
                {
                    SetUpVoiceKeyPhrases();

                    if (dontRequireDwellOnce && playerLookingAtCharacter)
                    {
                        canBeInteractedWith = true;
                        StartTalkingAttempt();
                    }
                }
                else
                {
                    canBeInteractedWith = false;
                    if (dialogueInteractionType != DialogueInteractionType.Speaking)
                        OnStopRecordingAttemptAction();
                }
            }

            if (dialogueInteractionType == DialogueInteractionType.Selection)
                canBeInteractedWith = false;

            if (audioSource.clip != null)
                //Wait for current clip to end + silence offset
                yield return new WaitForSeconds(audioSource.clip.length + secondsToAutoContinue);

            canBeInteractedWith = true;

            if (dialogueInteractionType == DialogueInteractionType.Selection)
            {
                if (camPointedObject != null && canBeInteractedWith)
                    camPointedObject.enabled = true;
            }


            //If it's single choice and autoContinue activated, go to Next Dialogue
            if (dialogue.DialogueType == DSDialogueType.SingleChoice && autoContinueSingleChoice)
            {
                GoToNextDialogue(0);
            }
        }

        private void SetUpVoiceKeyPhrases()
        {
            //Remove Previous VoskSTT KeyPhrases from all languages
            voskSTT.KeyPhrases.Clear(); //TODO May have to account for voice-command keyphrases (grab, place, etc.)
            voskSTT.KeyPhrasesEN.Clear(); //TODO May have to account for voice-command keyphrases (grab, place, etc.)
            voskSTT.KeyPhrasesES.Clear(); //TODO May have to account for voice-command keyphrases (grab, place, etc.)

            //Set the key phrases to Vosk STT based on multiple choices
            foreach (Data.DSDialogueChoiceData choice in dialogue.Choices)
            {
                switch (LanguageController.Instance.currentLanguage)
                {
                    case Languages.EN:
                        //Create array of formatted choices parsing commas
                        List<string> parsedChoicesEN = choice.TextEN.Split('/').ToList();
                        foreach (string choiceValue in parsedChoicesEN)
                        {
                            if (!string.IsNullOrEmpty(choiceValue))
                                voskSTT.KeyPhrasesEN.Add(choiceValue);
                        }
                        break;
                    case Languages.ES:
                        List<string> parsedChoicesES = choice.TextES.Split('/').ToList();
                        foreach (string choiceValue in parsedChoicesES)
                        {
                            if (!string.IsNullOrEmpty(choiceValue))
                                voskSTT.KeyPhrasesES.Add(choiceValue);
                        }
                        break;
                    case Languages.CA:
                        voskSTT.KeyPhrasesES.Add(choice.TextCA);
                        break;
                }
            }

            //Set current Keyphrases based on language
            switch (LanguageController.Instance.currentLanguage)
            {
                case Languages.EN:
                    voskSTT.KeyPhrases = voskSTT.KeyPhrasesEN;
                    break;
                case Languages.ES:
                    voskSTT.KeyPhrases = voskSTT.KeyPhrasesES;
                    break;
                case Languages.CA:
                    voskSTT.KeyPhrases = voskSTT.KeyPhrasesES;
                    break;
            }

            //Remove Current Vosk Recognizer to update Key Phrases/ Grammar
            voskSTT.RemoveRecognizer();
        }

        /// <summary>
        /// Updates the display with the current dialogue text and choices.
        /// </summary>
        private void DisplayTextCurrentDialogue()
        {
            if (dialogueDisplayText != null)
            {
                switch (LanguageController.Instance.currentLanguage)
                {
                    case Languages.EN:
                        dialogueDisplayText.text = dialogue.TextEN;
                        break;
                    case Languages.ES:
                        dialogueDisplayText.text = dialogue.TextES;
                        break;
                    case Languages.CA:
                        dialogueDisplayText.text = dialogue.TextCA;
                        break;
                }
            }

            //If there are choices, display them
            if (dialogue.DialogueType == DSDialogueType.MultipleChoice)
            {
                for (int i = 0; i < dialogue.Choices.Count; i++)
                {
                    choiceDisplayTexts[i].gameObject.SetActive(true);
                    switch (LanguageController.Instance.currentLanguage)
                    {
                        case Languages.EN:
                            choiceDisplayTexts[i].text = dialogue.Choices[i].TextEN;
                            break;
                        case Languages.ES:
                            choiceDisplayTexts[i].text = dialogue.Choices[i].TextES;
                            break;
                        case Languages.CA:
                            choiceDisplayTexts[i].text = dialogue.Choices[i].TextCA;
                            break;
                    }

                    //Create array of formatted choices parsing commas
                    List<string> parsedChoices = choiceDisplayTexts[i].text.Split('/').ToList();

                    if (parsedChoices.Count > 0)
                        choiceDisplayTexts[i].text = parsedChoices[0];
                }
            }
            else //Otherwise deactivate choice text
            {
                foreach (TMP_Text text in choiceDisplayTexts)
                    text.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Restarts the dialogue sequence from a specified starting point.
        /// </summary>
        public void RestartDialogue(bool allowRestart = false)
        {
            if (!allowRestart && dialogueStarted)
                return;

            if (sttMicController == null)
                FindObjectOfType<STTMicController>(true);
            sttMicController.SetCurrentDialogueSystem(this);

            dialogue = startingDialogue;
            DisplayTextCurrentDialogue();
            if (playAudioCurrentDialogue != null)
                StopCoroutine(playAudioCurrentDialogue);
            playAudioCurrentDialogue = StartCoroutine(PlayAudioCurrentDialogue());

            dialogueStarted = true;
        }

        public void RestartDialogue(string dialogueToStartFrom, bool allowRestart = false)
        {
            if (!allowRestart && dialogueStarted)
                return;

            if (sttMicController == null)
                FindObjectOfType<STTMicController>(true);
            sttMicController.SetCurrentDialogueSystem(this);

            dialogue = startingDialogue;

            //Dialogue from string
            if (dialogueToStartFrom != null)
            {
                foreach (DSDialogueSO currentDialogue in dialogueContainer.UngroupedDialogues)
                {
                    if (currentDialogue.name == dialogueToStartFrom)
                        dialogue = currentDialogue;
                }
            }

            DisplayTextCurrentDialogue();
            if (playAudioCurrentDialogue != null)
                StopCoroutine(playAudioCurrentDialogue);
            playAudioCurrentDialogue = StartCoroutine(PlayAudioCurrentDialogue());

            dialogueStarted = true;
        }

        /// <summary>
        /// Restarts the dialogue sequence from a specified starting point.
        /// </summary>
        private void DialogueFinished()
        {
            Debug.Log($"Dialogue {dialogueContainer.FileName} finished");
            OnDialogueFinishedEvent?.Invoke();
        }

        /// <summary>
        /// Changes the current dialogue container to a new one.
        /// </summary>
        /// <param name="newDialogueContainer">The new dialogue container to switch to.</param>
        public void ChangeDialogueContainer(DSDialogueContainerSO newDialogueContainer)
        {
            dialogueContainer = newDialogueContainer;
        }

        /// <summary>
        /// Checks the result of speech-to-text transcription and determines the appropriate response.
        /// </summary>
        /// <param name="result">The transcribed text.</param>
        /// <param name="isDictation">Flag indicating if the mode is dictation.</param>
        public void CheckTranscriptionResult(string result, bool isDictation)
        {
            //If not currently in a multiple choice dialogue, ignore
            if (dialogue.DialogueType != DSDialogueType.MultipleChoice)
                return;

            //Nothing was received (ignore in dictation mode as many results will be empty)
            if (string.IsNullOrEmpty(result) && !isDictation)
            {
                //Say that you have not heard the person
                StartCoroutine(DisplayDefaultAnswer(0));
                sttMicController.ResetDictationMode();
                return;
            }

            List<int> possibleMatchesIndexes = new List<int>();

            //Remove accents from STT Result!
            result = RemoveDiacritics(result);

            //Loop through all choice texts to find matches
            for (int i = 0; i < dialogue.Choices.Count; i++)
            {
                string formattedChoiceText = "";

                //Remove accents from choices!
                switch (LanguageController.Instance.currentLanguage)
                {
                    case Languages.EN:
                        formattedChoiceText = RemoveDiacritics(dialogue.Choices[i].TextEN);
                        break;
                    case Languages.ES:
                        formattedChoiceText = RemoveDiacritics(dialogue.Choices[i].TextES);
                        break;
                    case Languages.CA:
                        formattedChoiceText = RemoveDiacritics(dialogue.Choices[i].TextCA);
                        break;
                }

                if (string.IsNullOrEmpty(formattedChoiceText))
                {
                    Debug.Log("Choice Text for current language is empty");
                    //Therefore accept any answer
                    GoToNextDialogue(0);
                    dontRequireDwellOnce = true;
                    sttMicController.ForceRecordingOff();
                    return;
                }

                //Create array of formatted choices parsing commas
                List<string> parsedChoices = formattedChoiceText.Split('/').ToList();

                if (parsedChoices.Count > 0)
                {
                    for (int j = 0; j < parsedChoices.Count; j++)
                    {
                        if (result.Contains(parsedChoices[j], StringComparison.InvariantCultureIgnoreCase))
                        {
                            //Success! Add to match list
                            possibleMatchesIndexes.Add(i);
                        }
                    }
                }
                else
                {
                    if (result.Contains(formattedChoiceText, StringComparison.InvariantCultureIgnoreCase))
                    {
                        //Success! Add to match list
                        possibleMatchesIndexes.Add(i);
                    }
                }
            }

            //If more than one match, or none, ask again
            if (possibleMatchesIndexes.Count != 1 && !isDictation)
            {
                //Ask again, say the answer was not clear
                StartCoroutine(DisplayDefaultAnswer(1));
                sttMicController.ResetDictationMode();
                return;
            }
            else if (possibleMatchesIndexes.Count == 1) //If one match exactly, answer accepted, immediately even if dictation
            {
                //if (camPointedObject.triggerEntered)
                //    StartTalkingAttempt();
                GoToNextDialogue(possibleMatchesIndexes[0]);
                dontRequireDwellOnce = true;
                sttMicController.ForceRecordingOff();
            }
        }

        /// <summary>
        /// Displays a default answer based on the given index when the user's input is unclear or not detected.
        /// </summary>
        /// <param name="defaultAnswerIndex">The index of the default answer to use.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator DisplayDefaultAnswer(int defaultAnswerIndex)
        {
            //OnStopRecordingAttemptAction();
            sttMicController.currentlyRecording = false;
            canBeInteractedWith = false;
            yield return new WaitForSeconds(1);

            AudioClip defaultAnswerClip;
            string defaultAnswerText;

            switch (defaultAnswerIndex)
            {
                case 0:
                    //defaultAnswerClip = didNotHearClip;
                    defaultAnswerClip = (LanguageController.Instance.currentLanguage == Languages.EN) ? didNotHearClipEN : didNotHearClipES;
                    defaultAnswerText = "Perdona, no te he oído, ¿puedes volver a hablar?";
                    break;
                case 1:
                    defaultAnswerClip = (LanguageController.Instance.currentLanguage == Languages.EN) ? didNotUnderstandClipEN : didNotUnderstandClipES;
                    defaultAnswerText = "Disculpa, no te he entendido, ¿puedes responder de nuevo?";
                    break;
                default:
                    defaultAnswerClip = (LanguageController.Instance.currentLanguage == Languages.EN) ? didNotUnderstandClipEN : didNotUnderstandClipES;
                    defaultAnswerText = "Disculpa, no te he entendido, ¿puedes responder de nuevo?";
                    break;
            }

            //Display/say default answer
            dialogueDisplayText.text = defaultAnswerText;

            //Stop current clip if there was one
            audioSource.Stop();
            audioSource.clip = defaultAnswerClip;
            audioSource.Play();

            canBeInteractedWith = false;
            camPointedObject.enabled = false;

            //Wait for character to finish default line
            yield return new WaitForSeconds(defaultAnswerClip.length + 1);

            dontRequireDwellOnce = true;
            canBeInteractedWith = true;
            camPointedObject.enabled = true;

            //I added this to repeat the question, but maybe it's weird to do that

            //Go back to choice dialogue
            DisplayTextCurrentDialogue();

            if (playerLookingAtCharacter)
                StartTalkingAttempt();

            ////Stop then Start Audio
            StopCoroutine(PlayAudioCurrentDialogue());
            StartCoroutine(PlayAudioCurrentDialogue());
            yield break;
        }

        /// <summary>
        /// Public function wrapper. Plays AudioClip that is not part of a DialogueSystem, commonly used to play audios after specific events are triggered in the environment.
        /// </summary>
        /// <param name="audioClip">Audio to be played</param>
        /// <param name="audioText">Text to be displayed</param>
        public void PlayAudioOutsideOfSystem(Languages language, AudioClip audioClip, string audioText = "")
        {
            if (LanguageController.Instance.currentLanguage == language)
            {
                advanceLevelWhenInteracted = false;
                if (playAudioOutsideOfSystemCoroutine != null)
                    StopCoroutine(playAudioOutsideOfSystemCoroutine);
                playAudioOutsideOfSystemCoroutine = StartCoroutine(PlayAudioOutsideOfSystemCoroutine(audioClip));
            }
        }

        public void PlayAudioOutsideOfSystem(Languages language, AudioClip audioClip, string audioText = "", bool chainedInteractionDialogue = false)
        {
            if (LanguageController.Instance.currentLanguage == language)
            {
                advanceLevelWhenInteracted = false;
                if (playAudioOutsideOfSystemCoroutine != null)
                    StopCoroutine(playAudioOutsideOfSystemCoroutine);
                playAudioOutsideOfSystemCoroutine = StartCoroutine(PlayAudioOutsideOfSystemCoroutine(audioClip));
            }
        }

        public void PlayAudioOutsideOfSystem(Languages language, AudioClip audioClip, string audioText = "", bool chainedInteractionDialogue = false, bool autoAdvanceCurrentLevel = false)
        {
            if (LanguageController.Instance.currentLanguage == language)
            {
                advanceLevelWhenInteracted = autoAdvanceCurrentLevel;
                if (playAudioOutsideOfSystemCoroutine != null)
                    StopCoroutine(playAudioOutsideOfSystemCoroutine);
                playAudioOutsideOfSystemCoroutine = StartCoroutine(PlayAudioOutsideOfSystemCoroutine(audioClip));
            }
        }

        public void SetAudioForTalkingInteraction(Languages language, AudioClip audioClip, string audioText = "", bool autoAdvanceCurrentLevel = false)
        {
            if (LanguageController.Instance.currentLanguage == language)
            {
                audioWhenInteracted = audioClip;
                advanceLevelWhenInteracted = autoAdvanceCurrentLevel;
            }
        }

        /// <summary>
        /// Plays AudioClip that is not part of a DialogueSystem, commonly used to play audios after specific events are triggered in the environment.
        /// </summary>
        /// <param name="audioClip">Audio to be played</param>
        /// <param name="audioText">Text to be displayed</param>
        private IEnumerator PlayAudioOutsideOfSystemCoroutine(AudioClip audioClip, string audioText = "", bool chainedInteractionDialogue = false)
        {
            canBeInteractedWith = false;

            if (camPointedObject != null && dialogueInteractionType != DialogueInteractionType.Speaking)
                camPointedObject.enabled = false;

            yield return new WaitForSeconds(1);

            //Display text
            if (dialogueDisplayText != null)
                dialogueDisplayText.text = audioText;

            //Stop current clip if there was one
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.Play();

            //Wait for character to finish current line
            if (audioClip != null)
            {
                yield return new WaitForSeconds(audioClip.length);

                if (advanceLevelWhenInteracted)
                {
                    advanceLevelWhenInteracted = false;
                    LevelController.Instance.PlayNextLevel();
                }

                if (!chainedInteractionDialogue)
                    yield return new WaitForSeconds(1);
            }

            canBeInteractedWith = true;
            if (camPointedObject != null && dialogueInteractionType != DialogueInteractionType.Speaking)
                camPointedObject.enabled = true;

            if (chainedInteractionDialogue)
                StartCoroutine(PlayAudioOutsideOfSystemCoroutine(audioClip));
        }

        /// <summary>
        /// Removes diacritics from a given text, aiding in text comparison and processing.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <returns>The processed text with diacritics removed.</returns>
        static string RemoveDiacritics(string text)
        {
            string formD = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char ch in formD)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public void ChangeAutoContinueSingleChoice(bool newState)
        {
            autoContinueSingleChoice = newState;
        }

        public void StartTalkingAttempt()
        {
            if (sttMicController.currentlyRecording)
            {
                if (camPointedObject != null)
                {
                    camPointedObject.StopTimer();
                    camPointedObject.enabled = false;
                }
                return;
            }

            if (!canBeInteractedWith)// || audioWhenInteracted == null){}
            {
                if (camPointedObject != null)
                {
                    camPointedObject.StopTimer();
                    camPointedObject.enabled = false;
                }
                sttMicController.voiceCommandMode = false;
                //OnStopRecordingAttemptAction();
                return;
            }

            switch (dialogueInteractionType)
            {
                case DialogueInteractionType.Selection:
                    OnStartTalkingAttemptAction();
                    pendingTalkInteraction = true;
                    break;

                case DialogueInteractionType.Speaking:
                    //OnStartRecordingAttemptAction();
                    if (dontRequireDwellOnce)
                    {
                        if (camPointedObject != null)
                            camPointedObject.enabled = false;
                        OnStartRecordingAttemptAction();
                        dontRequireDwellOnce = false;
                    }
                    else
                    {
                        OnStartTalkingAttemptAction();
                        pendingTalkInteraction = true;
                    }
                    break;
            }
        }

        public void SetDialogueInteractionType(DialogueInteractionType _dialogueInteractionType)
        {
            dialogueInteractionType = _dialogueInteractionType;
        }

        public void StopTalkingAttempt()
        {
            if (!canBeInteractedWith)
                return;

            switch (dialogueInteractionType)
            {
                case DialogueInteractionType.Selection:
                    OnStopTalkingAttemptAction();
                    if (camPointedObject != null && canBeInteractedWith)
                        camPointedObject.enabled = true;
                    pendingTalkInteraction = false;
                    break;

                case DialogueInteractionType.Speaking:
                    sttMicController.fromDwellToRecord = false;

                    if (!sttMicController.currentlyRecording)
                    {
                        CrosshairController.Instance.ChangeToIdle();
                        sttMicController.voiceCommandMode = false;
                    }

                    if (camPointedObject != null)
                        camPointedObject.enabled = true;
                    break;
            }
        }

        public void SetDontRequireDwellOnce(bool newState)
        {
            dontRequireDwellOnce = newState;
        }

        private void OnTriggerEnter(Collider other)
        {
            CameraGrabber cameraGrabber = other.GetComponent<CameraGrabber>();

            if (cameraGrabber != null)
                playerLookingAtCharacter = true;
        }

        private void OnTriggerExit(Collider other)
        {
            CameraGrabber cameraGrabber = other.GetComponent<CameraGrabber>();

            if (cameraGrabber != null)
                playerLookingAtCharacter = false;
        }

        public void SetAdvanceLevelWhenInteracted(bool newState)
        {
            advanceLevelWhenInteracted = newState;
        }
    }
}
