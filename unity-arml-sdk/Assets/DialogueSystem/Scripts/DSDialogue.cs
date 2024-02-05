using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace DS
{
    using Enumerations;
    using ScriptableObjects;

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
        enum DialogueInteractionType
        {
            Selection,
            Speaking
        }

        //[SerializeField] private DialogueInteractionType dialogueInteractionType = DialogueInteractionType.Selection;

        [Tooltip("Automatically advances the dialogue for single-choice dialogues")]
        [SerializeField] private bool autoContinueSingleChoice;
        [SerializeField] private float secondsToAutoContinue;
        [SerializeField] private bool autoStartDialogue;

        // Default Answers
        [SerializeField] private AudioClip didNotHearClip;
        [SerializeField] private AudioClip didNotUnderstandClip;

        //STT Vosk
        private STTMicController sttMicController;

        // UnityEvent
        [SerializeField] private UnityEvent OnDialogueEventIndex1;
        [SerializeField] private UnityEvent OnDialogueEventIndex2;
        [SerializeField] private UnityEvent OnDialogueFinishedEvent;

        private AudioSource audioSource;
        private VoskSpeechToText voskSTT;

        /// <summary>
        /// Initializes dialogue settings and prepares for interaction.
        /// </summary>
        private void Start()
        {
            audioSource = GetComponent<AudioSource?>();
            sttMicController = FindObjectOfType<STTMicController>(true);
            voskSTT = FindObjectOfType<VoskSpeechToText>();

            if (sttMicController == null)
                Debug.LogError("No STTMicController found in the scene, make sure to add one for speech-to-text");

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

            //Remove text from displayTexts
            if (dialogueDisplayText != null)
                dialogueDisplayText.text = string.Empty;
            foreach (TMP_Text choiceDisplayText in choiceDisplayTexts)
                choiceDisplayText.gameObject.SetActive(false);

            if (autoStartDialogue)
                RestartDialogue();
        }

        /// <summary>
        /// Handles key inputs for debugging or specific interactions.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                //RestartDialogue();
            }
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
                    Debug.Log($"Dialogue Choice {dialogue.Choices[choiceIndex].Text} was selected");
                }

                //If placed here, it will happen right before next dialogue
                InvokeEventDialogue();

                dialogue = nextDialogue;
                DisplayTextCurrentDialogue();

                //If placed here, it will happen right when the dialogue appears
                //InvokeEventDialogue();

                //Stop then Start Audio
                StopCoroutine(PlayAudioCurrentDialogue());
                StartCoroutine(PlayAudioCurrentDialogue());
            }
            else
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
            if (dialogue.AudioClip == null || audioSource == null)
                yield break;

            //Stop current clip if there was one
            audioSource.Stop();
            audioSource.clip = dialogue.AudioClip;
            audioSource.Play();

            //Wait for current clip to end + silence offset
            yield return new WaitForSeconds(audioSource.clip.length + secondsToAutoContinue);

            //If it's single choice and autoContinue activated, go to Next Dialogue
            if (dialogue.DialogueType == DSDialogueType.SingleChoice && autoContinueSingleChoice)
            {
                yield return new WaitForSeconds(2f);
                GoToNextDialogue(0);
            }
            else if (dialogue.DialogueType == DSDialogueType.MultipleChoice)
            {
                //Remove Previous VoskSTT KeyPhrases
                voskSTT.KeyPhrases.Clear(); //TODO May have to account for voice-command keyphrases (grab, place, etc.)

                //Set the key phrases to Vosk STT based on multiple choices
                foreach (Data.DSDialogueChoiceData choice in dialogue.Choices)
                {
                    voskSTT.KeyPhrases.Add(choice.Text);
                }

                //Remove Current Vost Recognizer to update Key Phrases/Grammar
                voskSTT.RemoveRecognizer();

                //Start Auto-Recording when character has finished speaking his multiple choice line
                //sttMicController.ToggleRecording(6);
            }
        }

        /// <summary>
        /// Updates the display with the current dialogue text and choices.
        /// </summary>
        private void DisplayTextCurrentDialogue()
        {
            if (dialogueDisplayText != null)
                dialogueDisplayText.text = dialogue.Text;

            //If there are choices, display them
            if (dialogue.DialogueType == DSDialogueType.MultipleChoice)
            {
                for (int i = 0; i < dialogue.Choices.Count; i++)
                {
                    choiceDisplayTexts[i].gameObject.SetActive(true);
                    choiceDisplayTexts[i].text = dialogue.Choices[i].Text;
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
        public void RestartDialogue()
        {
            sttMicController.SetCurrentDialogueSystem(this);

            dialogue = startingDialogue;
            DisplayTextCurrentDialogue();
            StopCoroutine(PlayAudioCurrentDialogue());
            StartCoroutine(PlayAudioCurrentDialogue());
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

            //Loop through all choice texts to find matches
            for (int i = 0; i < dialogue.Choices.Count; i++)
            {
                //Remove accents!
                if (RemoveDiacritics(result).Contains(RemoveDiacritics(dialogue.Choices[i].Text), StringComparison.InvariantCultureIgnoreCase))
                {
                    //Success! Add to match list
                    possibleMatchesIndexes.Add(i);
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
                GoToNextDialogue(possibleMatchesIndexes[0]);
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
            yield return new WaitForSeconds(1);

            AudioClip defaultAnswerClip;
            string defaultAnswerText;

            switch (defaultAnswerIndex)
            {
                case 0:
                    defaultAnswerClip = didNotHearClip;
                    defaultAnswerText = "Perdona, no te he oído, ¿puedes volver a hablar?";
                    break;
                case 1:
                    defaultAnswerClip = didNotUnderstandClip;
                    defaultAnswerText = "Disculpa, no te he entendido, ¿puedes responder de nuevo?";
                    break;
                default:
                    defaultAnswerClip = didNotUnderstandClip;
                    defaultAnswerText = "Disculpa, no te he entendido, ¿puedes responder de nuevo?";
                    break;
            }

            //Display/say default answer
            dialogueDisplayText.text = defaultAnswerText;

            //Stop current clip if there was one
            audioSource.Stop();
            audioSource.clip = defaultAnswerClip;
            audioSource.Play();

            //Wait for character to finish default line
            yield return new WaitForSeconds(defaultAnswerClip.length + 3);

            //Optional, start recording
            sttMicController.ToggleRecording(6);

            yield break;

            //I added this to repeat the question, but maybe it's weird to do that

            //yield return new WaitForSeconds(defaultAnswerClip.length + 3);

            ////Go back to choice dialogue
            //DisplayTextCurrentDialogue();

            ////Stop then Start Audio
            //StopCoroutine(PlayAudioCurrentDialogue());
            //StartCoroutine(PlayAudioCurrentDialogue());
        }

        /// <summary>
        /// Public function wrapper. Plays AudioClip that is not part of a DialogueSystem, commonly used to play audios after specific events are triggered in the environment.
        /// </summary>
        /// <param name="audioClip">Audio to be played</param>
        /// <param name="audioText">Text to be displayed</param>
        public void PlayAudioOutsideOfSystem(AudioClip audioClip, string audioText = "")
        {
            StartCoroutine(PlayAudioOutsideOfSystemCoroutine(audioClip, audioText));
        }

        /// <summary>
        /// Plays AudioClip that is not part of a DialogueSystem, commonly used to play audios after specific events are triggered in the environment.
        /// </summary>
        /// <param name="audioClip">Audio to be played</param>
        /// <param name="audioText">Text to be displayed</param>
        private IEnumerator PlayAudioOutsideOfSystemCoroutine(AudioClip audioClip, string audioText = "")
        {
            yield return new WaitForSeconds(1);

            //Display text
            if (dialogueDisplayText != null)
                dialogueDisplayText.text = audioText;

            //Stop current clip if there was one
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.Play();

            //Wait for character to finish default line
            yield return new WaitForSeconds(audioClip.length + 3);
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
    }
}
