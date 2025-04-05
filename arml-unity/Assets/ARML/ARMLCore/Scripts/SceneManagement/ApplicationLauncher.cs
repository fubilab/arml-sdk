using ARML.Saving;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SpectacularAI;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace ARML.SceneManagement
{
    public enum TrackingMode
    {
        VioOnly,
        ImuOnly,
        VioPlusImu,
    }

    public enum ImuOrientation
    {
        XForward,
        XBackward,
    }
    /// <summary>
    /// Manages the launch of external applications from within a Unity application. 
    /// It dynamically creates UI elements to represent and launch these applications.
    /// </summary>
    public class ApplicationLauncher : MonoBehaviour
    {
        [SerializeField] GameObject appLaunchContainerPrefab;
        [SerializeField] RectTransform content;
        [SerializeField] string fileFormatExtension;
        [SerializeField] GameObject scrollView;
        [SerializeField] GameObject settingsPanel;
        
        public SettingsConfiguration settings;

        private List<string> applicationPathList = new List<string>();
        private string applicationsDirectory = "";

        private EventSystem eventSystem;
        private GameObject previouslySelected;

        private GameObject firstContainer;

        /// <summary>
        /// Initializes the Application Launcher, setting up directories and populating UI with application launch options.
        /// </summary>
        void Awake()
        {
            settings = SettingsConfiguration.LoadFromDisk();
            DirectoryInfo d = new DirectoryInfo(Application.dataPath);

            eventSystem = EventSystem.current;
            applicationsDirectory = d.Parent.Parent.FullName;
            fileFormatExtension = ".x86_64";

#if UNITY_EDITOR_WIN
            applicationsDirectory = $"{System.IO.Directory.GetCurrentDirectory()}\\_build\\";
            fileFormatExtension = ".exe";
#endif

            if (!Directory.Exists(applicationsDirectory))
                return;

            d = new DirectoryInfo(applicationsDirectory);


            FileInfo[] files = d.GetFiles($"*{fileFormatExtension}", SearchOption.AllDirectories);

            foreach (var file in d.GetFiles($"*{fileFormatExtension}", SearchOption.AllDirectories))
            {
                //Log file names
                //print(file.Directory?.Name);

                // skip if relative path starts with _
                string[] pathSplit = file.Directory.FullName
                    .Replace(applicationsDirectory, "")
                    .Split(Path.DirectorySeparatorChar);
                if (pathSplit.Any(dirname => dirname.StartsWith("_")))
                {
                    continue;
                }

                // skip if already in list
                if (applicationPathList.Contains(file.Directory.Name))
                {
                    continue;
                }

                //Add to list
                applicationPathList.Add(file.Directory.Name);

                //Remove format in string and display in container
                GameObject container = Instantiate(appLaunchContainerPrefab, content.transform);
                int dotIndex = file.Name.IndexOf('.');
                //container.GetComponentInChildren<TMP_Text>().text = file.Name.Substring(0, dotIndex);
                container.GetComponentInChildren<TMP_Text>().text = file.Directory.Name;
                container.GetComponent<Button>().onClick.AddListener(() => LoadApplication(file.FullName));

                Button button = container.GetComponent<Button>();

                //Set first one as selected
                if (applicationPathList.Count == 1)
                {
                    firstContainer = container;
                    eventSystem.firstSelectedGameObject = firstContainer;
                }
            }

            //Now that all apps are added to list, activate ContentSizeFitter
            content.GetComponent<ContentSizeFitter>().enabled = true;

            //Lastly add TeamViewer as an option
            //string teamViewerPath = "/opt/teamviewer/tv_bin/script/teamviewer";
#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR
        //string teamViewerPath = "/usr/bin/teamviewer";
        //if (!File.Exists(teamViewerPath))
        //{
        //    return;
        //}

        //GameObject twContainer = Instantiate(appLaunchContainerPrefab, content.transform);
        //twContainer.GetComponentInChildren<TMP_Text>().text = "TeamViewer";
        //twContainer.GetComponent<Button>().onClick.AddListener(() => LoadApplication(teamViewerPath));
#endif
        }

        void OnApplicationFocus(bool focus)
        {
            Arduino.ArduinoController.Instance.enabled = focus;
        }

        /// <summary>
        /// Makes a file at the specified path executable.
        /// </summary>
        /// <param name="filePath">The file path of the application to launch.</param>
        void MakeExecutable(string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "/bin/bash",
                Arguments = "-c \" chmod +x " + filePath + "\"",
                CreateNoWindow = true
            };
            Process proc = new Process() { StartInfo = startInfo };
            proc.Start();
            proc.WaitForExit();
        }

        /// <summary>
        /// Launches an external application given its file path.
        /// </summary>
        /// <param name="filePath">The file path of the application to launch.</param>
        void LoadApplication(string filePath)
        {
            Arduino.ArduinoController.Instance.SetArduinoReady(false);
            if (File.Exists(filePath))
            {
                MakeExecutable(filePath);
                Process.Start(filePath);
            }
        }

        public void QuitLauncher()
        {
            Application.Quit();
        }

        private void UpdateSettingsUI()
        {
            TMP_Text zOffsetText = GameObject.Find("Z Offset/Value")?.GetComponent<TMP_Text>();
            zOffsetText.text = settings.zOffset.ToString();
        }

    public void ToggleSettingsPanel()
        {
            scrollView.SetActive(!scrollView.activeInHierarchy);
            settingsPanel.SetActive(!settingsPanel.activeInHierarchy);
            if (settingsPanel.activeInHierarchy)
                UpdateSettingsUI();
        }
    
        public void IncreaseZOffset()
        {
            settings.zOffset += 0.5f;
            SaveSettings();
        }
        
        public void DecreaseZOffset()
        {
            settings.zOffset -= 0.5f;
            SaveSettings();
        }

        public void SaveSettings()
        {
            settings.SaveToDisk();
            UpdateSettingsUI();
        }
    }

    [System.Serializable]
    public class SettingsConfiguration
    {
        [DefaultValue(false)] public bool displayLog;
        [DefaultValue(false)] public bool displayScan;
        [DefaultValue(0)] public int languageIndex;
        [DefaultValue(null)] public List<VioParameter> vioInternalParameters;
        [DefaultValue(0)] public float zOffset;
        [DefaultValue(TrackingMode.VioOnly)] public TrackingMode trackingMode;
        [DefaultValue(ImuOrientation.XBackward)] public ImuOrientation imuOrientation;
        [DefaultValue(false)] public bool useSlam;
        [DefaultValue(KeyCode.Menu)] public KeyCode menuKey;
        [DefaultValue(KeyCode.PageDown)] public KeyCode menuKey2;
        [DefaultValue(KeyCode.Backspace)] public KeyCode resetKey;
        
        public static string ConfigFilePath {
            get => $"{Application.persistentDataPath}/launcherSettings.json";
        }

        private static SettingsConfiguration _settingsConfiguration;

        public static SettingsConfiguration LoadFromDisk(bool forceLoad = false)
        {
            if (_settingsConfiguration != null && !forceLoad)
            {
                return _settingsConfiguration;
            }
            if (!File.Exists(ConfigFilePath))
            {
                Debug.LogWarning("[CONFIG] Settings file not found, using default configuration.");
                return new SettingsConfiguration();
            };
            try
            {
                IDataService dataService = new JsonDataService();
                Debug.Log($"[CONFIG] Settings file loaded from {ConfigFilePath}");
                var settings = dataService.LoadData<SettingsConfiguration>(ConfigFilePath, false);
                return settings;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CONFIG] Error loading settings from: {ConfigFilePath}\n" + e.ToString());
                Debug.LogWarning("[CONFIG] Using default configuration");
            }

            return new SettingsConfiguration();
        }
        public bool SaveToDisk()
        {
            try
            {
                IDataService dataService = new JsonDataService();
                dataService.SaveData(ConfigFilePath, this, false);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CONFIG] Error saving settings to: {ConfigFilePath}\n" + e.ToString());
            }

            return false;
        }
    }
}