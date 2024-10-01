using System;
using UnityEngine;

namespace DS.Data
{
    using ScriptableObjects;

    [Serializable]
    public class DSDialogueChoiceData
    {
        [field: SerializeField] public string TextEN { get; set; }
        [field: SerializeField] public string TextES { get; set; }
        [field: SerializeField] public string TextCA { get; set; }
        [field: SerializeField] public DSDialogueSO NextDialogue { get; set; }
    }
}