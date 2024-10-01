using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Records and plays back position and rotation data for a GameObject. 
    /// Allows data to be saved to and loaded from a JSON file.
    /// </summary>
    public class ExportPositionData : MonoBehaviour
    {
        [SerializeField, Tooltip("Recordings per second")]
        private float recordRate = 60f; // Records per second

        [SerializeField]
        private Mode mode = Mode.RECORD; // Select mode in the Inspector

        [SerializeField, Tooltip("Path to the playback data file within Assets/")]
        private string playbackFilePath; // Path to the playback data file

        private List<CamPositionData> positionDataList = new List<CamPositionData>();

        private enum Mode
        {
            RECORD,
            PLAYBACK
        }

        /// <summary>
        /// Called before the first frame update. Starts either recording or playback routine based on the selected mode.
        /// </summary>
        private void Start()
        {
            switch (mode)
            {
                case Mode.RECORD:
                    StartCoroutine(RecordPositionRoutine());
                    break;
                case Mode.PLAYBACK:
                    StartCoroutine(PlaybackPositionRoutine());
                    break;
            }
        }

        /// <summary>
        /// Coroutine that handles the position recording at a defined rate.
        /// </summary>
        private IEnumerator RecordPositionRoutine()
        {
            while (true)
            {
                RecordPosition();
                yield return new WaitForSeconds(1 / recordRate);
            }
        }

        /// <summary>
        /// Records the current position and rotation of the GameObject.
        /// </summary>
        private void RecordPosition()
        {
            CamPositionData data = new CamPositionData
            {
                realTimeSinceStartup = Time.realtimeSinceStartup,
                posX = transform.localPosition.x,
                posY = transform.localPosition.y,
                posZ = transform.localPosition.z,
                rotX = transform.localEulerAngles.x,
                rotY = transform.localEulerAngles.y,
                rotZ = transform.localEulerAngles.z
            };

            positionDataList.Add(data);
        }

        /// <summary>
        /// Coroutine that handles the playback of recorded position data.
        /// </summary>
        private IEnumerator PlaybackPositionRoutine()
        {
            string filePath = Application.dataPath + "/" + playbackFilePath;
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                var playbackData = JsonConvert.DeserializeObject<List<CamPositionData>>(jsonData);
                if (playbackData != null)
                {
                    foreach (var data in playbackData)
                    {
                        transform.localPosition = new Vector3(data.posX, data.posY, data.posZ);
                        transform.localEulerAngles = new Vector3(data.rotX, data.rotY, data.rotZ);
                        yield return new WaitForSeconds(1 / recordRate);
                    }
                }
            }
            else
            {
                Debug.LogError($"Playback file not found at {filePath}");
            }
        }

        /// <summary>
        /// Serializes the recorded position data into a JSON string.
        /// </summary>
        /// <returns>A JSON string representing the recorded data.</returns>
        public string SerializePositionData()
        {
            return JsonConvert.SerializeObject(positionDataList, Formatting.Indented);
        }


        /// <summary>
        /// Saves the recorded data to a JSON file.
        /// </summary>
        void SaveData()
        {
            string jsonData = SerializePositionData();
            string filePath = $"{Application.dataPath}/SavedData/ExportPositionData_{recordRate}FPS.json";
            File.WriteAllText(filePath, jsonData);
            Debug.Log($"Saved data to {filePath}");
        }

        /// <summary>
        /// Called once per frame. Checks for user input to stop recording and save the data.
        /// </summary>
        private void Update()
        {
            if (mode == Mode.RECORD && Input.GetKey(KeyCode.P))
            {
                StopCoroutine(RecordPositionRoutine());
                SaveData();
            }
        }
    }

    [Serializable]
    public struct CamPositionData
    {
        public double realTimeSinceStartup;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ;
    }
}