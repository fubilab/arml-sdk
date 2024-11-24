using System.Diagnostics;
using System.IO;
using UnityEngine;
using ARML.DebugTools;
using ARML.Saving;
using ARML.Language;

namespace ARML.SceneManagement
{
    /// <summary>
    /// Handles game loading and global settings
    /// </summary>
    public class GameController : MonoBehaviour
    {
        #region Singleton
        /// <summary>
        /// Singleton instance of GameController.
        /// </summary>
        public static GameController Instance { get; private set; }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            // If there is an instance, and it's not me, delete myself.
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        #endregion

        public bool loadOnStart;
        public bool loadLauncherSettings;

        [SerializeField] DebugCanvasController debugCanvasController;

        private int currentScore;
        private float currentTime;

        [HideInInspector]
        public bool displayScanAtStart;
        private bool gameAlreadyLoaded;

        private IDataService DataService = new JsonDataService();
        private SettingsConfiguration settings;

        private void Start()
        {
            if (loadLauncherSettings)
                LoadLauncherSettings();
#if !UNITY_EDITOR            
            if (loadOnStart)
            {
                debugCanvasController.ToggleAllDebug();
                LoadGame();
            }
#endif
        }

        private void SetAprilTagsPath()
        {
            //string command = @"/home/fubintlab/spectacular.sh " + aprilTagsPath;
            ////string command = $"/usr/bin/gnome-terminal -- bash -c \"/home/fubintlab/spectacular.sh {aprilTagsPath}; bash\"";
            //print(command);
            //Process.Start(command);

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "/home/fubintlab/spectacular.sh",
                UseShellExecute = true,
                CreateNoWindow = false,
                Arguments = "dogtags"
            };
            Process myProcess = new Process
            {
                StartInfo = startInfo
            };
            myProcess.Start();
            myProcess.WaitForExit();
        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.L))
            {
                LoadGame();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void LoadGame()
        {
            print("LoadGame");
            if (!gameAlreadyLoaded)
            {
                StartCoroutine(SceneController.Instance.LoadSceneByIndex(1));
                gameAlreadyLoaded = true;
            }
        }

        private void LoadLauncherSettings()
        {
            string path = $"{Application.persistentDataPath}/launcherSettings.json";

            settings = DataService.LoadData<SettingsConfiguration>(path, false);

            LanguageController.Instance.SetLanguage(settings.languageIndex);
            debugCanvasController.SetScreenLogger(settings.displayLog);
            displayScanAtStart = settings.displayScan;
            debugCanvasController.SetMapRenderer(displayScanAtStart);
        }
    }
}