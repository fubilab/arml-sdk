using ARML.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using ARML.Saving;
using ARML.SceneManagement;

namespace SpectacularAI.DepthAI
{
    public enum AccelerometerFrequency
    {
        Hz125 = 125,
        Hz250 = 250,
        Hz500 = 500,
    }

    public enum GyroscopeFrequency
    {
        Hz100 = 100,
        Hz200 = 200,
        Hz400 = 400,
        Hz1000 = 1000,
    }

    /// <summary>
    /// Provides higher level API to Pipeline and Session.
    /// VIO session. Should be created via Pipeline::StartSession.
    /// </summary>
    public class Vio : MonoBehaviour
    {
        [Tooltip("Use mapping API")]
        public bool MappingAPI = false;

        [Tooltip("When enabled, outputs pose at very low latency on every IMU sample instead of camera frame.")]
        public bool LowLatency = true;

        [Tooltip("When true, use stereo camera for tracking. When false, use mono.")]
        public bool UseStereo = true;

        [Tooltip("Set SLAM (simultaneous-location-and-mapping) module enabled")]
        public bool UseSlam = false;

        [Tooltip("Set OAK-D feature tracker enabled.")]
        public bool UseFeatureTracker = true;

        [Tooltip("Use more lightweight parameters.")]
        public bool FastVio = false;

        [Tooltip("Native options: 125, 250, 500.")]
        public AccelerometerFrequency AccFrequencyHz = AccelerometerFrequency.Hz500;

        [Tooltip("Native options: 100, 200, 400, 1000")]
        public GyroscopeFrequency GyroFrequencyHz = GyroscopeFrequency.Hz400;

        [Tooltip("If enabled, VIO doesn't produce any outputs")]
        public bool RecordingOnly = false;

        [Tooltip("If not empty, the session will be recorded to the given folder")]
        public string RecordingFolder = "";

        [Tooltip("Path to .json file with AprilTag information. AprilTag detection is enabled when not empty.\n" +
            "For the file format see: https://github.com/SpectacularAI/docs/blob/main/pdf/april_tag_instructions.pdf\n" +
            "Note: sets useSlam=true")]
        [SerializeField]
        public string AprilTagPath = "";

        [Tooltip("Internal algorithm parameters")]
        public List<VioParameter> InternalParameters;

        [Tooltip("Read Launcher Settings to check for internal paramaters")]
        public bool readLauncherSettings;

        private Pipeline _pipeline;
        private Session _session;
        private SettingsConfiguration launcherSettings;
        private IDataService DataService = new JsonDataService();

        /// <summary>
        /// The current vio output.
        /// </summary>
        public static VioOutput Output { get; private set; }

        private void OnEnable()
        {
            Configuration config = new Configuration();
            config.LowLatency = LowLatency;
            config.UseStereo = UseStereo;
            config.UseSlam = UseSlam;
            config.UseFeatureTracker = UseFeatureTracker;
            config.FastVio = FastVio;
            config.AccFrequencyHz = (uint)AccFrequencyHz;
            config.GyroFrequencyHz = (uint)GyroFrequencyHz;
            config.RecordingOnly = RecordingOnly;
            config.RecordingFolder = RecordingFolder;
            config.AprilTagPath = AprilTagPath;

            if (readLauncherSettings)
            {
                string path = $"{Application.persistentDataPath}/launcherSettings.json";
                launcherSettings = DataService.LoadData<SettingsConfiguration>(path, false);

                foreach (var internalParameter in launcherSettings.vioInternalParameters)
                {
                    InternalParameters.Add(new VioParameter(
                        internalParameter[0],
                        internalParameter[1]
                        ));
                }
            }

#if UNITY_EDITOR
            return;
#endif
            _pipeline = new Pipeline(configuration: config, enableMappingAPI: MappingAPI, internalParameters: InternalParameters.ToArray());
            _session = _pipeline.StartSession();
        }

        public void OnDisable()
        {
#if UNITY_EDITOR
            return;
#endif
            _session.Dispose();
            _pipeline.Dispose();
            _session = null;
            _pipeline = null;
        }

        private void Update()
        {
#if UNITY_EDITOR
            return;
#endif

            // Dispose previous vio output
            if (Output != null)
            {
                Output.Dispose();
                Output = null;
            }

            if (_session != null && _session.HasOutput())
            {
                Output = _session.GetOutput();
            }
        }
    }
}