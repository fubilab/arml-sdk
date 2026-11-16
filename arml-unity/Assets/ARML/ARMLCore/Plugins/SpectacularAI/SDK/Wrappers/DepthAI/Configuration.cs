using System.Runtime.InteropServices;

namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// Plugin and Spectacular AI VIO SDK configuration variables.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public sealed class Configuration
    {
        /// <summary>
        /// When true, use stereo camera for tracking. When false, use mono.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseStereo = true;

        /// <summary>
        /// Set SLAM (simultaneous-location-and-mapping) module enabled.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseSlam = false;

        /// <summary>
        /// Set OAK-D feature tracker enabled.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseFeatureTracker = true;

        /// <summary>
        /// Use more lightweight parameters.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool FastVio = false;

        /// <summary>
        /// Should be true when device has color stereo cameras
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseColorStereoCameras = false;

        /// <summary>
        /// Enable the color camera. Always enabled when hand tracking is enabled.
        /// NOTE: this field must stay in this position, the layout must match ConfigurationWrapper in the native plugin.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseColor = false;

        /// <summary>
        /// SLAM map save path. Supported formats are .bin (internal SLAM map format) and point cloud formats .ply and .csv
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string MapSavePath = "";

        /// <summary>
        /// SLAM map load path (only .bin format).
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string MapLoadPath = "";

        /// <summary>
        /// Path to .json file with AprilTag information. AprilTag detection is enabled when not empty.
        /// For the file format see: https://github.com/SpectacularAI/docs/blob/main/pdf/april_tag_instructions.pdf
        /// Note: sets useSlam=true
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string AprilTagPath = "";

        /// <summary>
        /// Native options: 125, 250, 500
        /// </summary>
        public uint AccFrequencyHz = 500;

        /// <summary>
        /// Native options: 100, 200, 400, 1000
        /// </summary>
        public uint GyroFrequencyHz = 400;

        /// <summary>
        /// When useSlam = true, useFeatureTracker = true and keyframeCandidateEveryNthFrame > 0,
        /// a mono gray image is captured every N frames for SLAM.
        /// </summary>
        public int KeyframeCandidateEveryNthFrame = 6;

        /// <summary>
        /// valid options: 400p, 800p, 1200p
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string InputResolution = "400p";

        /// <summary>
        /// If not empty, the session will be recorder to the given folder
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string RecordingFolder = "";

        /// <summary>
        /// Disables VIO and only records session when recordingFolder is set
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool RecordingOnly = false;

        /// <summary>
        /// Set true to disable IMU batching.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool FastImu = false;

        /// <summary>
        /// When enabled, outputs pose at very low latency on every IMU sample instead of camera frame.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool LowLatency = false;

        /// <summary>
        /// Run MediaPipe hand tracking on the OAK-D next to the VIO. Requires the palm detection and hand landmark
        /// models (blob files, see HandTrackingPalmModelPath). Results are read with Session.GetHandFrame.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool EnableHandTracking = false;

        /// <summary>
        /// Path to palm_detection_sh4.blob
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string HandTrackingPalmModelPath = "";

        /// <summary>
        /// Path to hand_landmark_full_sh4.blob (or the lite / sparse variant)
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string HandTrackingLandmarkModelPath = "";

        /// <summary>
        /// Use stereo depth to calibrate the user's hand size, making hand positions metric. The right camera of the
        /// device only sees IR light, so this only works with the IR dot projector enabled.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool HandTrackingUseDepth = true;

        /// <summary>
        /// IR dot projector intensity, 0 = off, 1 = maximum. Only used when HandTrackingUseDepth is set.
        /// </summary>
        public float HandTrackingIrProjectorIntensity = 0.0f;

        /// <summary>
        /// Maximum number of hands, 1 or 2
        /// </summary>
        public int HandTrackingMaxHands = 2;

        /// <summary>
        /// Frame rate of the color camera when hand tracking is enabled, 0 = unchanged. A lower rate makes the
        /// camera cooler and the neural networks faster.
        /// </summary>
        public int HandTrackingColorFps = 0;
    }
}
