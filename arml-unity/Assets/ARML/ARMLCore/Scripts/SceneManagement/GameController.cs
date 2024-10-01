using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace ARML
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

        public SceneField gameSceneToLoad;
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
        }

        public void LoadGame()
        {
            if (!gameAlreadyLoaded)
            {
                StartCoroutine(SceneController.Instance.LoadSceneByName(gameSceneToLoad));
                gameAlreadyLoaded = true;
            }
        }

        private void LoadLauncherSettings()
        {
            string path = Directory.GetParent(Application.persistentDataPath).FullName;
            path = Directory.GetParent(path).FullName + "/UPF/ARMLLauncher/settings.json";

            settings = DataService.LoadData<SettingsConfiguration>(path, false);

            LanguageController.Instance.SetLanguage(settings.languageIndex);
            debugCanvasController.SetScreenLogger(settings.displayLog);
            displayScanAtStart = settings.displayScan;
            debugCanvasController.SetMapRenderer(displayScanAtStart);
        }
    }
}