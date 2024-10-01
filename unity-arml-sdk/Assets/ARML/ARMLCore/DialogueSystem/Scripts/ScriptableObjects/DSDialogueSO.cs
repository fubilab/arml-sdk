using System.Collections.Generic;
using UnityEngine;

namespace DS.ScriptableObjects
{
    using Data;
    using Enumerations;

    public class DSDialogueSO : ScriptableObject
    {
        [field: SerializeField] public string DialogueName { get; set; }
        [field: SerializeField][field: TextArea()] public string TextEN { get; set; }
        [field: SerializeField][field: TextArea()] public string TextES { get; set; }
        [field: SerializeField][field: TextArea()] public string TextCA { get; set; }
        [field: SerializeField] public List<DSDialogueChoiceData> Choices { get; set; }
        [field: SerializeField] public DSDialogueType DialogueType { get; set; }
        [field: SerializeField] public bool IsStartingDialogue { get; set; }
        [field: SerializeField] public bool IsEndingDialogue { get; set; }
        [field: SerializeField] public AudioClip AudioClipEN { get; set; }
        [field: SerializeField] public AudioClip AudioClipES { get; set; }
        [field: SerializeField] public AudioClip AudioClipCA { get; set; }
        [field: SerializeField] public int EventID { get; set; }

        public void Initialize(string dialogueName, string textEN, string textES, string textCA, List<DSDialogueChoiceData> choices, DSDialogueType dialogueType, 
            bool isStartingDialogue, bool isEndingDialogue, AudioClip audioClipEN = null, AudioClip audioClipES = null, AudioClip audioClipCA = null, int eventID = 0)
        {
            DialogueName = dialogueName;
            TextEN = textEN;
            TextES = textES;
            TextCA = textCA;
            Choices = choices;
            DialogueType = dialogueType;
            IsStartingDialogue = isStartingDialogue;
            IsEndingDialogue = isEndingDialogue;
            AudioClipEN = audioClipEN;
            AudioClipES = audioClipES;
            AudioClipCA = audioClipCA;
            EventID = eventID;
        }
    }
}