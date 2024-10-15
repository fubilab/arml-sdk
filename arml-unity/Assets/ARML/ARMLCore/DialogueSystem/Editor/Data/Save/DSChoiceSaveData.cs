using System;
using UnityEngine;

namespace ARML.DS.Data.Save
{
    [Serializable]
    public class DSChoiceSaveData
    {
        [field: SerializeField] public string TextEN { get; set; }
        [field: SerializeField] public string TextES { get; set; }
        [field: SerializeField] public string TextCA { get; set; }
        [field: SerializeField] public string NodeID { get; set; }
    }
}