using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace ARML
{
    /// <summary>
    /// Logs position and rotation data from CSV files, spawning heat map points based on raycast hits.
    /// </summary>
    public class LogPositionGraph : MonoBehaviour
    {
        [Tooltip("Relative path to the CSV file in the Resources folder.")]
        [SerializeField] string csvFolderPath; // Relative path to the CSV file in the Resources folder.

        [Tooltip("Scale factor for the time between entries.")]
        [SerializeField] float timeScale = 1.0f; // Scale factor for the time between entries.

        [Tooltip("If true, spawns heat map points on raycast hits.")]
        [SerializeField] bool spawnHeatMap; // If true, spawns heat map points on raycast hits.

        [Tooltip("Prefab to spawn at the hit position if a heat map point is not present.")]
        [SerializeField] GameObject heatMapPointPrefab; // Prefab to spawn at the hit position.

        [Tooltip("Layer to hit with the raycast.")]
        [SerializeField] LayerMask layerMask; // Layer to hit with the raycast.

        [Tooltip("Offset applied to the position of the logged data.")]
        [SerializeField] Vector3 positionOffset; // Offset applied to the position of the logged data.

        [Tooltip("Offset applied to the rotation of the logged data.")]
        [SerializeField] Vector3 rotationOffset; // Offset applied to the rotation of the logged data.

        private List<DataEntry> dataEntries; // List to hold the position and rotation data.
        private float startTime; // Time at which data logging starts.
        private DirectoryInfo info; // Directory info for accessing CSV files.

        /// <summary>
        /// Class representing a single data entry with time, position, and rotation.
        /// </summary>
        [Serializable]
        private class DataEntry
        {
            public float TimeSinceStartUp; // Time since the application started.
            public Vector3 Position; // Position of the object.
            public Vector3 Rotation; // Rotation of the object.

            public DataEntry(float timeSinceStartUp, Vector3 position, Vector3 rotation)
            {
                TimeSinceStartUp = timeSinceStartUp;
                Position = position;
                Rotation = rotation;
            }
        }

        /// <summary>
        /// Initializes the log position graph by loading CSV files and starting the update coroutine.
        /// </summary>
        void Start()
        {
            dataEntries = new List<DataEntry>();

            // Get all files in the specified folder.
            string path = Path.Combine(Application.dataPath, csvFolderPath);
            info = new DirectoryInfo(path);
            var fileInfo = info.GetFiles();
            foreach (FileInfo file in fileInfo)
            {
                LoadCSV(file.FullName);
            }

            startTime = Time.time; // Store the start time for calculating elapsed time.
            StartCoroutine(UpdatePositionAndRotation());
        }

        /// <summary>
        /// Loads data from the specified CSV file and populates the dataEntries list.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to load.</param>
        void LoadCSV(string filePath)
        {
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    // Skip the first line (header).
                    if (Array.IndexOf(lines, line) == 0)
                        continue;

                    string[] values = line.Split(',');

                    if (values.Length == 8)
                    {
                        float timeSinceStartUp = float.Parse(values[1]);
                        float posX = float.Parse(values[2]);
                        float posY = float.Parse(values[3]);
                        float posZ = float.Parse(values[4]);
                        float rotX = float.Parse(values[5]);
                        float rotY = float.Parse(values[6]);
                        float rotZ = float.Parse(values[7]);

                        Vector3 position = new Vector3(posX, posY, posZ);
                        Vector3 rotation = new Vector3(rotX, rotY, rotZ);

                        dataEntries.Add(new DataEntry(timeSinceStartUp, position, rotation));
                    }
                }
            }
            else
            {
                Debug.LogError("CSV file not found at path: " + filePath);
            }
        }

        /// <summary>
        /// Coroutine that updates the position and rotation of the object based on the loaded data entries.
        /// </summary>
        /// <returns>Enumerator for coroutine functionality.</returns>
        IEnumerator UpdatePositionAndRotation()
        {
            int index = 0;
            while (index < dataEntries.Count)
            {
                DataEntry entry = dataEntries[index];
                float elapsedTime = (Time.time - startTime) * timeScale;

                if (elapsedTime >= entry.TimeSinceStartUp)
                {
                    // Update position and rotation based on the data entry.
                    transform.position = entry.Position + positionOffset;
                    transform.eulerAngles = entry.Rotation + rotationOffset;

                    // Perform raycast
                    RaycastHit hit;
                    Vector3 forward = transform.forward;
                    Vector3 startPosition = transform.position;
                    if (Physics.Raycast(startPosition, forward, out hit, Mathf.Infinity, layerMask))
                    {
                        if (spawnHeatMap)
                            HeatMapSpawn(hit);

                        // Draw a line to show the raycast.
                        Debug.DrawLine(startPosition, hit.point, Color.green, 0.05f);
                    }
                    else
                    {
                        // Draw a ray to show the direction.
                        Debug.DrawRay(startPosition, forward * 1000, Color.red, 0.05f);
                    }

                    if (index % 10000 == 0)
                        Debug.Log($"DataEntry {index} out of {dataEntries.Count}");

                    index++;
                }

                yield return null; // Wait until the next frame.
            }
        }

        /// <summary>
        /// Handles spawning of heat map points or prefabs at the raycast hit position.
        /// </summary>
        /// <param name="hit">The RaycastHit containing information about the hit point.</param>
        private void HeatMapSpawn(RaycastHit hit)
        {
            HeatMapPoint point = hit.transform.gameObject.GetComponent<HeatMapPoint>();

            if (point != null)
            {
                point.PointHit(dataEntries.Count);
            }
            else
            {
                // Spawn the prefab at the hit position.
                if (heatMapPointPrefab != null)
                    Instantiate(heatMapPointPrefab, hit.point, Quaternion.identity);
            }
        }
    }
}
