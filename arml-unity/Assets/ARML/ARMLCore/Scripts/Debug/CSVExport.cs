using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ARML.DebugTools
{
    /// <summary>
    /// CSVExport is responsible for recording and exporting data to CSV files. 
    /// It tracks both user actions and camera positions over time.
    /// </summary>
    public class CSVExport : MonoBehaviour
    {
        /// <summary>
        /// The rate at which the camera's position will be recorded, in frames per second.
        /// </summary>
        [Tooltip("The rate at which the camera's position will be recorded, in frames per second.")]
        [SerializeField] float camRecordRate = 30;

        /// <summary>
        /// List of monitored actions performed by the user.
        /// </summary>
        [Tooltip("List of monitored actions for export.")]
        public List<MonitoredAction> monitoredActions = new List<MonitoredAction>();

        private List<CamPositionData> cameraData = new List<CamPositionData>();
        private string actionsFilePath;
        private string camFilePath;
        private Camera cam;

        /// <summary>
        /// Singleton instance of the CSVExport class.
        /// </summary>
        public static CSVExport Instance { get; private set; }

        void SetSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy the GameObject if an instance already exists
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Optionally make it persistent across scenes
            }
        }

        private void Awake()
        {
            SetSingleton();
        }

        /// <summary>
        /// Initializes file paths and starts recording the camera position at the beginning.
        /// </summary>
        private void Start()
        {
            // Setting the file path to save the CSV in the Unity project folder
            actionsFilePath = Path.Combine(Application.persistentDataPath,
                $"{ParticipantCodeController.Instance.UniqueID}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.csv");

            camFilePath = Path.Combine(Application.persistentDataPath,
                $"{ParticipantCodeController.Instance.UniqueID}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}_CamData.csv");

            cam = Camera.main;

            StartCoroutine(RecordPositionRoutine());
        }

        /// <summary>
        /// Exports both actions data and camera position data.
        /// </summary>
        public void ExportData()
        {
            ExportActionsData();
            ExportCamData();
        }

        /// <summary>
        /// Exports the monitored user actions to a CSV file.
        /// </summary>
        private void ExportActionsData()
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(actionsFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Using 'using' statement for automatic disposal
            using (StreamWriter writer = new StreamWriter(actionsFilePath, false))
            {
                // Write a header line including the ID
                writer.WriteLine("ID,TimeStamp,ActionType,ActionObject");

                foreach (var action in monitoredActions)
                {
                    // Format each MonitoredAction as a CSV row, now including the ID
                    string line = $"{ParticipantCodeController.Instance.UniqueID},{action.TimeStamp},{action.ActionType},{action.ActionObject}";
                    writer.WriteLine(line);
                }
            }

            // Save Unique ID to not repeat it
            ParticipantCodeController.Instance.SaveUsedID();

            Debug.Log("Exported file to " + actionsFilePath);
        }

        /// <summary>
        /// Exports the recorded camera position data to a CSV file.
        /// </summary>
        private void ExportCamData()
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(camFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Using 'using' statement for automatic disposal
            using (StreamWriter writer = new StreamWriter(camFilePath, false))
            {
                // Write a header line including the ID
                writer.WriteLine("ID,TimeSinceStartUp,PosX,PosY,PosZ,RotX,RotY,RotZ");

                foreach (var camData in cameraData)
                {
                    // Format each CamPositionData as a CSV row, now including the ID
                    string line = $"{ParticipantCodeController.Instance.UniqueID}," +
                        $"{camData.realTimeSinceStartup}," +
                        $"{camData.posX}," +
                        $"{camData.posY}," +
                        $"{camData.posZ}," +
                        $"{camData.rotX}," +
                        $"{camData.rotY}," +
                        $"{camData.rotZ}";
                    writer.WriteLine(line);
                }
            }

            Debug.Log("Exported file to " + camFilePath);
        }

        /// <summary>
        /// Coroutine that continuously records the camera's position at a fixed rate.
        /// </summary>
        /// <returns>IEnumerator for coroutine handling.</returns>
        private IEnumerator RecordPositionRoutine()
        {
            while (true)
            {
                RecordPosition();
                yield return new WaitForSeconds(1 / camRecordRate);
            }
        }

        /// <summary>
        /// Records the current position and rotation of the camera.
        /// </summary>
        private void RecordPosition()
        {
            if (cam == null)
            {
                cam = Camera.main;
                return;
            }

            CamPositionData data = new CamPositionData
            {
                realTimeSinceStartup = Time.realtimeSinceStartup,
                posX = cam.transform.position.x,
                posY = cam.transform.position.y,
                posZ = cam.transform.position.z,
                rotX = cam.transform.eulerAngles.x,
                rotY = cam.transform.eulerAngles.y,
                rotZ = cam.transform.eulerAngles.z
            };

            cameraData.Add(data);
        }
    }

    /// <summary>
    /// Represents a user action that is monitored and exported.
    /// </summary>
    [Serializable]
    public struct MonitoredAction
    {
        /// <summary>
        /// Enum representing different types of user actions.
        /// </summary>
        public enum ActionTypeEnum
        {
            HOVER,
            UNHOVER,
            GRABBED,
            STARTED,
            COMPLETED,
            PROGRESSED,
            USED
        }

        /// <summary>
        /// The type of action being monitored.
        /// </summary>
        [Tooltip("The type of action being monitored.")]
        public ActionTypeEnum ActionType;

        /// <summary>
        /// The timestamp when the action occurred.
        /// </summary>
        [Tooltip("The timestamp when the action occurred.")]
        public string TimeStamp;

        /// <summary>
        /// The name or description of the object associated with the action.
        /// </summary>
        [Tooltip("The name or description of the object associated with the action.")]
        public string ActionObject;

        /// <summary>
        /// Constructor for MonitoredAction.
        /// </summary>
        /// <param name="actionType">The type of action.</param>
        /// <param name="timeStamp">The time the action occurred.</param>
        /// <param name="actionObject">The object the action was performed on.</param>
        public MonitoredAction(ActionTypeEnum actionType, string timeStamp, string actionObject)
        {
            ActionType = actionType;
            TimeStamp = timeStamp;
            ActionObject = actionObject;
        }
    }
}
