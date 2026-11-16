using ARML.AprilTags;
using ARML.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public string AprilTagPath = "";

        [Tooltip("Path to .bin file with saved SLAM map.")]
        [SerializeField]
        public string MapLoadPath = "";


        [Tooltip("Internal algorithm parameters")]
        public List<VioParameter> InternalParameters;

        [Tooltip("Read Launcher Settings to check for internal paramaters")]
        public bool readLauncherSettings;

        [Tooltip("Run the VIO (and hand tracking) in the Unity Editor. The OAK-D must be connected, and no other " +
            "program may use it. Builds always run it.")]
        public bool RunInEditor = true;

        [Header("Hand tracking")]
        [Tooltip("Run MediaPipe hand tracking on the OAK-D next to the VIO (see HandTracker). Needs the model files " +
            "in StreamingAssets. Also enabled by 'useHandTracking' in the launcher settings.")]
        public bool EnableHandTracking = false;

        [Tooltip("Folder inside StreamingAssets with the hand tracking models")]
        public string HandTrackingModelFolder = "HandTracking";

        public string PalmModelFile = "palm_detection_sh4.blob";

        [Tooltip("hand_landmark_full is the most accurate. hand_landmark_lite is faster, hand_landmark_sparse is in between.")]
        public string LandmarkModelFile = "hand_landmark_full_sh4.blob";

        [Tooltip("Use stereo depth to make hand positions metric. Needs the IR projector, which may affect the VIO.")]
        public bool HandTrackingUseDepth = false;

        [Tooltip("Intensity of the OAK-D IR dot projector, used for the stereo depth of hand tracking."), Range(0f, 1f)]
        public float HandTrackingIrProjectorIntensity = 0f;

        [Tooltip("Maximum number of tracked hands"), Range(1, 2)]
        public int HandTrackingMaxHands = 2;

        [Tooltip("Frame rate of the color camera for hand tracking. 15 keeps the camera cooler and makes the networks " +
            "faster, the hand tracking does not run faster than that anyway. 0 = 30 fps."), Range(0, 30)]
        public int HandTrackingColorFps = 15;

        /// <summary>
        /// Hand tracking result of this frame, null when there is no new result.
        /// </summary>
        public static HandFrame HandOutput { get; private set; }

        /// <summary>
        /// History of the RGB camera pose (in Unity world coordinates) of the VIO. Only filled when hand tracking is enabled.
        /// </summary>
        public static CameraPoseHistory CameraPoses { get; } = new CameraPoseHistory(512);

        /// <summary>
        /// True when the hand tracking is running
        /// </summary>
        public static bool HandTrackingActive { get; private set; }

        /// <summary>
        /// Transforms points from the (Unity) camera coordinates of the RGB camera to the (Unity) camera coordinates
        /// of the VIO output pose, which is what drives the camera object (PoseProvider). Constant, estimated from the
        /// VIO outputs. Identity until known, see RgbToOutputKnown.
        /// </summary>
        public static Matrix4x4 RgbToOutput { get; private set; } = Matrix4x4.identity;

        public static bool RgbToOutputKnown { get; private set; }

        /// <summary>Chip temperature of the camera in Celsius (0 = unknown). Hand tracking only.</summary>
        public static float DeviceTemperature { get; private set; }

        /// <summary>
        /// 0 = hand tracking at full speed, 1..3 = the camera is hot and hand tracking is slowed down to cool it
        /// </summary>
        public static int HandTrackingThrottleLevel { get; private set; }

        /// <summary>True when the hand tracking networks stopped answering, for example because the camera overheated</summary>
        public static bool HandTrackingStalled { get; private set; }

        /// <summary>
        /// The running session, null when the VIO is not running. For camera access such as Session.GetColorFrame.
        /// Main thread only.
        /// </summary>
        public static Session ActiveSession { get; private set; }

        private float _nextStatusPoll;

        private bool _handTrackingEnabled;

        private Pipeline _pipeline;
        private Session _session;
        private SettingsConfiguration _launcherSettings;

        [Tooltip("Start VIO Session on component Start, rather than waiting for the game scene to load.\n" + 
            "Do not set this if using AprilTags in your scene.")]
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
            else
            {
                if (SceneController.Instance.LoadedGameScene.isLoaded)
                {
                    GameSceneLoaded(SceneController.Instance.LoadedGameScene);
                }
                else
                {
                    SceneController.Instance.OnGameSceneLoaded += GameSceneLoaded;
                }
            }
        }

        private void GameSceneLoaded(Scene scene)
        {
            StartSession();
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
            config.MapLoadPath = MapLoadPath;

            bool useHandTracking = EnableHandTracking;
            if (readLauncherSettings)
            {
                // the launcher can enable hand tracking, but never disables it when set in the scene
                useHandTracking |= SettingsConfiguration.LoadFromDisk().useHandTracking;
            }
            if (useHandTracking)
            {
                string palmPath = Path.Combine(Application.streamingAssetsPath, HandTrackingModelFolder, PalmModelFile);
                string landmarkPath = Path.Combine(Application.streamingAssetsPath, HandTrackingModelFolder, LandmarkModelFile);
                if (!File.Exists(palmPath) || !File.Exists(landmarkPath))
                {
                    Debug.LogError($"[VIO] Hand tracking models not found ({palmPath}, {landmarkPath}), hand tracking disabled.");
                }
                else
                {
                    config.EnableHandTracking = true;
                    config.UseColor = true;
                    config.HandTrackingPalmModelPath = palmPath;
                    config.HandTrackingLandmarkModelPath = landmarkPath;
                    config.HandTrackingUseDepth = HandTrackingUseDepth;
                    config.HandTrackingIrProjectorIntensity = HandTrackingIrProjectorIntensity;
                    config.HandTrackingMaxHands = HandTrackingMaxHands;
                    config.HandTrackingColorFps = HandTrackingColorFps;
                    _handTrackingEnabled = true;
                    Debug.Log($"[VIO] Hand tracking enabled (depth={HandTrackingUseDepth}, IR={HandTrackingIrProjectorIntensity}).");
                }
            }

            if (!String.IsNullOrEmpty(AprilTagPath)) 
            {
                // override tag generation
                config.AprilTagPath = AprilTagPath;
                Debug.Log(@"[VIO] StartSession called with " + AprilTagPath + ".");
            }
            else
            {
                // look for AprilTag JSON in the 
                AprilTag[] aprilTags = FindObjectsByType<AprilTag>(FindObjectsSortMode.None);
                Debug.Log(@"[VIO] AprilTags " + aprilTags.Length);
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

                config.UseSlam = UseSlam = _launcherSettings.useSlam;

                if (_launcherSettings.vioInternalParameters == null)
                {
                    InternalParameters.Add(
                        new VioParameter() { Key = "trackerMasks", Value = "0.314,0.347,0.700,0.748" }
                    );
                }
                else
                {
                    foreach (var internalParameter in _launcherSettings.vioInternalParameters)
                    {
                        InternalParameters.Add(internalParameter);
                    }
                }
            }

            if (Application.isEditor && !RunInEditor)
            {
                Debug.Log("[VIO] Not started in the Editor (Run In Editor is off).");
                _handTrackingEnabled = false;
                return;
            }
            Debug.Log("[VIO] StartSession");
            try
            {
                _pipeline = new Pipeline(configuration: config, enableMappingAPI: MappingAPI, internalParameters: InternalParameters.ToArray());
                _session = _pipeline.StartSession();
            }
            catch (Exception e)
            {
                Debug.LogError("[VIO] Could not start the VIO session. Is the OAK-D connected and not used by another program?\n" + e.Message);
                if (_session != null) _session.Dispose();
                if (_pipeline != null) _pipeline.Dispose();
                _session = null;
                _pipeline = null;
                _handTrackingEnabled = false;
                return;
            }
            HandTrackingActive = _handTrackingEnabled;
            ActiveSession = _session;
            CameraPoses.Clear();
        }

        public void OnDisable()
        {
            ActiveSession = null;
            if (_session != null)
                _session.Dispose();
            if (_pipeline != null)
                _pipeline.Dispose();
            
            _session = null;
            _pipeline = null;
            HandTrackingActive = false;
            HandOutput = null;
            RgbToOutputKnown = false;
            HandTrackingStalled = false;
            HandTrackingThrottleLevel = 0;
            _handTrackingEnabled = false;
        }

        private void Update()
        {
            // Dispose previous vio output
            if (Output != null)
            {
                Output.Dispose();
                Output = null;
            }

            HandOutput = null;

            if (_session != null && _handTrackingEnabled)
            {
                UpdateWithHandTracking();
            }
            else if (_session != null && _session.HasOutput())
            {
                Output = _session.GetOutput();
            }
        }

        private void PollHandTrackingStatus()
        {
            if (!_session.TryGetHandTrackingStatus(out float temperature, out int level, out bool stalled)) return;
            if (stalled && !HandTrackingStalled)
            {
                Debug.LogWarning($"[VIO] Hand tracking stalled: the camera does not answer (chip temperature {temperature:0} C).");
            }
            else if (level != HandTrackingThrottleLevel && level > 0)
            {
                Debug.LogWarning($"[VIO] Camera chip at {temperature:0} C, hand tracking slowed down (level {level}).");
            }
            DeviceTemperature = temperature;
            HandTrackingThrottleLevel = level;
            HandTrackingStalled = stalled;
        }

        // With hand tracking all VIO outputs are consumed every frame: the camera pose of every output is needed to
        // find the pose at the capture time of the hand tracking frames. Output is the newest one.
        private void UpdateWithHandTracking()
        {
            VioOutput latest = null;
            while (_session.HasOutput())
            {
                VioOutput output = _session.GetOutput();
                if (output == null) break;
                if (output.Status == TrackingStatus.TRACKING)
                {
                    using (CameraPose cameraPose = _session.GetRgbCameraPose(output))
                    {
                        Matrix4x4 rgbToWorld = cameraPose.GetCameraToWorldMatrix();
                        CameraPoses.Add(output.Pose.Time, rgbToWorld);
                        // the RGB camera is not at the pose of the output (left camera, a few cm away)
                        RgbToOutput = output.Pose.AsMatrix().inverse * rgbToWorld;
                        RgbToOutputKnown = true;
                    }
                }
                if (latest != null) latest.Dispose();
                latest = output;
            }
            Output = latest;

            try
            {
                HandOutput = _session.GetHandFrame();
                if (Time.unscaledTime >= _nextStatusPoll)
                {
                    _nextStatusPoll = Time.unscaledTime + 1f;
                    PollHandTrackingStatus();
                }
            }
            catch (EntryPointNotFoundException)
            {
                Debug.LogError("[VIO] The native spectacularAI_unity plugin has no hand tracking support, disabling hand tracking.");
                _handTrackingEnabled = false;
                HandTrackingActive = false;
            }
        }
    }
}