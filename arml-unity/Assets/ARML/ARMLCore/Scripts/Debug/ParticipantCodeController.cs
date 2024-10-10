using UnityEngine;
using System.Collections.Generic;
using TMPro;
using NaughtyAttributes;

namespace ARML.DebugTools
{
    public class ParticipantCodeController : MonoBehaviour
    {
        [SerializeField] TMP_Text codeTextSmall;
        [SerializeField] TMP_Text codeTextBig;

        public int UniqueID { get; private set; }
        private HashSet<int> usedIDs = new HashSet<int>();

        public static ParticipantCodeController Instance { get; private set; }

        void SetSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy the GameObject if an instance already exists
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Optionally make it persistent
            }
        }

        private void Awake()
        {
            SetSingleton();
            LoadUsedIDs();
            GenerateUniqueID();
        }

        public int GenerateUniqueID()
        {
            int newID;
            do
            {
                newID = Random.Range(0, 1000); // Generate a number between 000 to 999
            }
            while (usedIDs.Contains(newID));

            usedIDs.Add(newID);
            //SaveUsedID(newID);

            codeTextSmall.text = newID.ToString();
            codeTextBig.text = newID.ToString();

            UniqueID = newID;

            return newID;
        }

        private void LoadUsedIDs()
        {
            string savedIDs = PlayerPrefs.GetString("UsedIDs", string.Empty);
            if (!string.IsNullOrEmpty(savedIDs))
            {
                foreach (var id in savedIDs.Split(','))
                {
                    if (int.TryParse(id, out int parsedID))
                    {
                        usedIDs.Add(parsedID);
                    }
                }
            }
        }

        public void SaveUsedID()
        {
            string savedIDs = PlayerPrefs.GetString("UsedIDs", string.Empty);

            if (string.IsNullOrEmpty(savedIDs))
            {
                PlayerPrefs.SetString("UsedIDs", UniqueID.ToString());
            }
            else
            {
                PlayerPrefs.SetString("UsedIDs", savedIDs + "," + UniqueID.ToString());
            }
            PlayerPrefs.Save();

            codeTextBig.gameObject.SetActive(true);
            codeTextSmall.gameObject.SetActive(true);
        }

        [Button]
        private void DeleteStoredIDs()
        {
            PlayerPrefs.DeleteKey("UsedIDs");
        }
    }
}