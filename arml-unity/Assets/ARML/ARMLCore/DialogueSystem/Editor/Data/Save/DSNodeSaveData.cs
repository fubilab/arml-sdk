using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARML.DS.Data.Save
{
    using Enumerations;

    [Serializable]
    public class DSNodeSaveData
    {
        [field: SerializeField] public string ID { get; set; }
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public string TextEN { get; set; }
        [field: SerializeField] public string TextES { get; set; }
        [field: SerializeField] public string TextCA { get; set; }
        [field: SerializeField] public List<DSChoiceSaveData> Choices { get; set; }
        [field: SerializeField] public string GroupID { get; set; }
        [field: SerializeField] public DSDialogueType DialogueType { get; set; }
        [field: SerializeField] public Vector2 Position { get; set; }
        [field: SerializeField] public AudioClip AudioClipEN { get; set; }
        [field: SerializeField] public AudioClip AudioClipES { get; set; }
        [field: SerializeField] public AudioClip AudioClipCA { get; set; }
        [field: SerializeField] public int EventID { get; set; }
    }
}