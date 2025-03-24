using ARML.AprilTags;
using ARML.SceneManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        [Tooltip("Path to .json file with AprilTag information. If set, overrides the automatic generation \n" +
            "based on placed AprilTag components in the scene.")]
        [SerializeField]
        public string AprilTagPath = "";

        [Tooltip("Internal algorithm parameters")]
        public List<VioParameter> InternalParameters;

        [Tooltip("Read Launcher Settings to check for internal paramaters")]
        public bool readLauncherSettings;

        private Pipeline _pipeline;
        private Session _session;
        private SettingsConfiguration _launcherSettings;

        [Tooltip("Start VIO Session on component Start")]
        public bool autoStartSession;

        /// <summary>
        /// The current vio output.
        /// </summary>
        public static VioOutput Output { get; private set; }

        void Start() 
        {
            if (autoStartSession) 
            {
                StartSession();
            }
        }

        public void StartSession()
        {
            if (_session != null)
            {
                Debug.LogWarning("[VIO] StartSession called when session already started, skipping.");
                return;
            }
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

            if (!String.IsNullOrEmpty(AprilTagPath)) 
            {
                // override tag generation
                config.AprilTagPath = AprilTagPath;
            }
            else
            {
                // look for AprilTag JSON in the 
                AprilTag[] aprilTags = FindObjectsByType<AprilTag>(FindObjectsSortMode.None);
                if (aprilTags.Length > 0) 
                {
                    string jsonPath = ARML.AprilTags.Utility.SerializeAprilTags(aprilTags);
                    config.AprilTagPath = jsonPath;
                    AprilTagPath = jsonPath;
                }
            }

            if (readLauncherSettings)
            {
                _launcherSettings = SettingsConfiguration.LoadFromDisk();

                foreach (var internalParameter in _launcherSettings.vioInternalParameters)
                {
                    InternalParameters.Add(internalParameter);
                }
            }

#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
            return;
#endif
            Debug.Log("[VIO] StartSession");
            _pipeline = new Pipeline(configuration: config, enableMappingAPI: MappingAPI, internalParameters: InternalParameters.ToArray());
            _session = _pipeline.StartSession();
        }

        public void OnDisable()
        {
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
            return;
#endif
            _session.Dispose();
            _pipeline.Dispose();
            _session = null;
            _pipeline = null;
        }

        private void Update()
        {
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
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