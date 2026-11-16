using System;
using SpectacularAI.DepthAI;
using UnityEngine;

namespace ARML.HandTracking
{
    public enum Hand
    {
        Left,
        Right
    }

    /// <summary>
    /// One tracked hand in Unity world coordinates. The instances are owned and updated by <see cref="HandTracker"/>.
    /// </summary>
    public sealed class TrackedHandPose
    {
        public Hand Hand { get; internal set; }

        /// <summary>True while the hand is tracked</summary>
        public bool Tracked { get; internal set; }

        /// <summary>The 21 joints in Unity world coordinates, indexed by <see cref="HandJoint"/></summary>
        public readonly Vector3[] Joints = new Vector3[HandData.JointCount];

        /// <summary>
        /// The recognized gesture. It is stable: it only changes after the new gesture was seen in several results in a
        /// row (<see cref="HandTracker.gestureConfirmResults"/>). None when the hand is not tracked.
        /// </summary>
        public HandGesture Gesture { get; internal set; }

        /// <summary>Gesture of the latest result before stabilization. It can flicker between results.</summary>
        public HandGesture RawGesture { get; internal set; }

        internal HandGesture GestureCandidate;
        internal int GestureCandidateCount;

        /// <summary>Confidence of the landmark model, 0..1</summary>
        public float Confidence { get; internal set; }

        /// <summary>True when the position is metric, i.e. the hand size was calibrated with stereo depth</summary>
        public bool DepthCalibrated { get; internal set; }

        /// <summary>Size of the user's hand relative to the model's average hand (1 = no size calibration)</summary>
        public float HandScale { get; internal set; }

        /// <summary>Raw stereo depth measured at the palm in meters, 0 = not measured</summary>
        public float StereoDepth { get; internal set; }

        /// <summary>Capture time of the camera image (VIO clock, seconds)</summary>
        public double CaptureTime { get; internal set; }

        /// <summary>Unity time (Time.unscaledTime) of the last update</summary>
        public float LastUpdateTime { get; internal set; }

        public Vector3 Wrist { get { return Joints[(int)HandJoint.Wrist]; } }

        public Vector3 GetJoint(HandJoint joint)
        {
            return Joints[(int)joint];
        }

        /// <summary>Center of the palm (average of wrist and the finger base joints)</summary>
        public Vector3 PalmCenter
        {
            get
            {
                return (Joints[(int)HandJoint.Wrist] + Joints[(int)HandJoint.IndexMcp] +
                    Joints[(int)HandJoint.MiddleMcp] + Joints[(int)HandJoint.RingMcp] +
                    Joints[(int)HandJoint.PinkyMcp]) / 5f;
            }
        }

        /// <summary>
        /// Distance across the image between thumb tip and index finger tip relative to the size of the hand (wrist to
        /// the base of the middle finger). 0.6 or more for an open hand, about 0.2 for a fist and 0.02 to 0.14 for a
        /// pinch. It does not depend on the hand size calibration, unlike <see cref="PinchDistance"/>.
        /// </summary>
        public float PinchRatio { get; internal set; }

        /// <summary>0..1: 1 when the tips of thumb and index finger touch, 0 when they are far apart. For analog use.</summary>
        public float PinchStrength { get; internal set; }

        /// <summary>The point between thumb tip and index tip in world coordinates, where to grab</summary>
        public Vector3 PinchPoint
        {
            get { return (Joints[(int)HandJoint.ThumbTip] + Joints[(int)HandJoint.IndexTip]) * 0.5f; }
        }

        internal bool Pinching;

        /// <summary>Distance between thumb tip and index finger tip in meters. Includes the depth, which is noisy.</summary>
        public float PinchDistance
        {
            get { return Vector3.Distance(Joints[(int)HandJoint.ThumbTip], Joints[(int)HandJoint.IndexTip]); }
        }

        internal readonly OneEuroVector3[] Filters = CreateFilters();

        private static OneEuroVector3[] CreateFilters()
        {
            var filters = new OneEuroVector3[HandData.JointCount];
            for (int i = 0; i < filters.Length; i++) filters[i] = new OneEuroVector3();
            return filters;
        }
    }

    /// <summary>
    /// Provides tracked hands in Unity world coordinates from the hand tracking running on the OAK-D camera,
    /// next to the Spectacular AI VIO. Enable <see cref="Vio.EnableHandTracking"/> to use it.
    ///
    /// The hand positions are computed in the coordinates of the camera at the capture time of the image, moved to the
    /// current camera pose using the VIO, and then transformed with the pose of the camera object, so they are
    /// consistent with whatever drives the camera (VIO, IMU or both).
    /// </summary>
    public class HandTracker : MonoBehaviour
    {
        public static HandTracker Instance { get; private set; }

        [Header("Camera")]
        [Tooltip("The PoseProvider that moves the camera object. Its transform is used to place the hands in the world.")]
        public PoseProvider poseProvider;

        [Tooltip("Used when there is no PoseProvider. Defaults to the main camera.")]
        public Transform cameraTransform;

        [Header("Hands")]
        [Tooltip("The model classifies hands as seen in the image. Enable if left and right are swapped for the " +
            "way the camera is mounted.")]
        public bool swapHandedness = false;

        [Tooltip("Hands with a lower landmark confidence are ignored."), Range(0f, 1f)]
        public float minConfidence = 0.6f;

        [Tooltip("A hand is reported as not tracked when it was not seen for this long (seconds).")]
        public float lostTimeout = 0.3f;

        [Header("Gestures")]
        [Tooltip("A new gesture is reported after it was seen in this many hand tracking results in a row. Higher values " +
            "flicker less but react later (a result takes about 70 ms)."), Range(1, 10)]
        public int gestureConfirmResults = 3;

        [Tooltip("A pinch starts when the distance between thumb tip and index tip across the image, relative to the size " +
            "of the hand, is below this (0.02 to 0.14 when pinching, about 0.2 for a fist, 0.6 or more for an open hand)."),
            Range(0.05f, 0.4f)]
        public float pinchEnterRatio = 0.14f;

        [Tooltip("A pinch ends when that ratio is above this. Higher than the start value so that it does not flicker."),
            Range(0.1f, 0.6f)]
        public float pinchExitRatio = 0.2f;

        [Header("Filtering (One Euro filter)")]
        public bool smooth = true;

        [Tooltip("Smoothing when the hand is still. Lower values remove more jitter but add more delay."), Range(0.1f, 10f)]
        public float smoothMinCutoff = 1.5f;

        [Tooltip("How fast the smoothing is reduced when the hand moves fast. Higher values reduce the delay."), Range(0f, 10f)]
        public float smoothBeta = 1.0f;

        public TrackedHandPose Left { get; } = new TrackedHandPose { Hand = Hand.Left };
        public TrackedHandPose Right { get; } = new TrackedHandPose { Hand = Hand.Right };

        /// <summary>True when the hand tracking is running (Vio.EnableHandTracking and the VIO session started)</summary>
        public bool Active { get { return Vio.HandTrackingActive; } }

        /// <summary>Chip temperature of the camera in Celsius</summary>
        public float DeviceTemperature { get { return Vio.DeviceTemperature; } }

        /// <summary>0 = full speed, 1..3 = hand tracking is slowed down because the camera is hot</summary>
        public int ThrottleLevel { get { return Vio.HandTrackingThrottleLevel; } }

        /// <summary>True when the hand tracking stopped answering (overheated camera)</summary>
        public bool Stalled { get { return Vio.HandTrackingStalled; } }

        /// <summary>Latency from capturing the image to receiving the hand tracking result in ms (diagnostics)</summary>
        public double LatencyMs { get; private set; }

        /// <summary>Number of hand tracking results received per second (diagnostics)</summary>
        public float ResultsPerSecond { get; private set; }

        /// <summary>Raised after a new hand tracking result was processed</summary>
        public event Action<HandTracker> Updated;

        /// <summary>
        /// Raised when the stable gesture of a hand changes: the hand and its previous gesture (the new one is
        /// <see cref="TrackedHandPose.Gesture"/>). A hand that is lost changes to None.
        /// </summary>
        public event Action<TrackedHandPose, HandGesture> GestureChanged;

        private int _resultsInWindow;
        private float _windowStart;

        private void OnEnable()
        {
            Instance = this;
            _windowStart = Time.unscaledTime;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            LoseHand(Left);
            LoseHand(Right);
        }

        private void LoseHand(TrackedHandPose pose)
        {
            pose.Tracked = false;
            pose.RawGesture = HandGesture.None;
            pose.GestureCandidate = HandGesture.None;
            pose.GestureCandidateCount = 0;
            pose.Pinching = false;
            pose.PinchRatio = 1f;
            pose.PinchStrength = 0f;
            SetGesture(pose, HandGesture.None);
        }

        private void SetGesture(TrackedHandPose pose, HandGesture gesture)
        {
            if (pose.Gesture == gesture) return;
            HandGesture previous = pose.Gesture;
            pose.Gesture = gesture;
            GestureChanged?.Invoke(pose, previous);
        }

        // Thumb tip to index tip distance relative to the hand size, with hysteresis. Measured on the metric camera
        // landmarks of the result, not on the smoothed joints, which lag. Only the distance across the image (camera x
        // and y) counts: the depth difference of the two tips is not reliable (about half a palm length apart in depth
        // while touching, on a recording of pinches), so with it a pinch could not be told from other poses.
        private void UpdatePinch(TrackedHandPose pose, HandData hand, bool newHand)
        {
            if (newHand) pose.Pinching = false;
            float palmLength = Vector3.Distance(
                hand.CameraLandmarks[(int)HandJoint.Wrist], hand.CameraLandmarks[(int)HandJoint.MiddleMcp]);
            Vector3 tips = hand.CameraLandmarks[(int)HandJoint.ThumbTip] - hand.CameraLandmarks[(int)HandJoint.IndexTip];
            float ratio = palmLength > 0.01f ? new Vector2(tips.x, tips.y).magnitude / palmLength : 1f;
            pose.PinchRatio = ratio;
            pose.PinchStrength = Mathf.Clamp01(Mathf.InverseLerp(pinchExitRatio * 2f, pinchEnterRatio, ratio));

            float exit = Mathf.Max(pinchExitRatio, pinchEnterRatio);
            if (!pose.Pinching && ratio < pinchEnterRatio) pose.Pinching = true;
            else if (pose.Pinching && ratio > exit) pose.Pinching = false;
        }

        // Switches the stable gesture only after the same new gesture was seen in several results in a row
        private void UpdateGesture(TrackedHandPose pose, HandGesture raw, bool newHand)
        {
            pose.RawGesture = raw;
            if (newHand)
            {
                pose.GestureCandidate = raw;
                pose.GestureCandidateCount = 0;
            }
            if (raw == pose.Gesture)
            {
                pose.GestureCandidate = raw;
                pose.GestureCandidateCount = 0;
                return;
            }
            if (raw == pose.GestureCandidate) pose.GestureCandidateCount++;
            else
            {
                pose.GestureCandidate = raw;
                pose.GestureCandidateCount = 1;
            }
            if (pose.GestureCandidateCount >= Mathf.Max(1, gestureConfirmResults))
            {
                pose.GestureCandidateCount = 0;
                SetGesture(pose, raw);
            }
        }

        private void Update()
        {
            HandFrame frame = Vio.HandOutput;
            if (frame != null)
            {
                ProcessFrame(frame);
                LatencyMs = frame.LatencyMs;
                _resultsInWindow++;
            }

            float now = Time.unscaledTime;
            if (now - _windowStart >= 1f)
            {
                ResultsPerSecond = _resultsInWindow / (now - _windowStart);
                _resultsInWindow = 0;
                _windowStart = now;
            }

            if (Left.Tracked && now - Left.LastUpdateTime > lostTimeout) LoseHand(Left);
            if (Right.Tracked && now - Right.LastUpdateTime > lostTimeout) LoseHand(Right);
        }

        /// <summary>The camera transform used to place the hands in the world</summary>
        public Transform CameraObject { get { return ResolveCamera(); } }

        private Transform ResolveCamera()
        {
            if (poseProvider != null) return poseProvider.transform;
            if (cameraTransform != null) return cameraTransform;
            return Camera.main != null ? Camera.main.transform : null;
        }

        private void ProcessFrame(HandFrame frame)
        {
            Transform cam = ResolveCamera();
            if (cam == null) return;

            // Camera motion between the capture of the image and now, in Unity camera coordinates
            Matrix4x4 frameToNow = Matrix4x4.identity;
            if (Vio.CameraPoses.TryGet(frame.Timestamp, out Matrix4x4 cameraAtFrame) &&
                Vio.CameraPoses.TryGetLatest(out _, out Matrix4x4 cameraNow))
            {
                frameToNow = cameraNow.inverse * cameraAtFrame;
            }

            // PoseProvider adds a rotation offset to the camera object (but not in BNO mode)
            Quaternion inverseOffset = Quaternion.identity;
            if (poseProvider != null && !poseProvider.UseOrientationFromBNO)
            {
                inverseOffset = Quaternion.Inverse(Quaternion.Euler(poseProvider.rotationOffset));
            }

            Matrix4x4 rgbToOutput = Vio.RgbToOutputKnown ? Vio.RgbToOutput : Matrix4x4.identity;

            float now = Time.unscaledTime;
            foreach (HandData hand in frame.Hands)
            {
                if (!hand.Placed || hand.LandmarkScore < minConfidence) continue;

                bool right = hand.IsRightHand != swapHandedness;
                TrackedHandPose pose = right ? Right : Left;

                // Start filters again when the hand was lost for a while
                bool restart = !pose.Tracked;
                double dt = pose.Tracked ? frame.Timestamp - pose.CaptureTime : 0.033;
                if (dt <= 0.0) dt = 0.033;

                for (int k = 0; k < HandData.JointCount; k++)
                {
                    // RGB camera at the capture time -> RGB camera now -> pose of the VIO output -> camera object
                    Vector3 local = inverseOffset * rgbToOutput.MultiplyPoint3x4(
                        frameToNow.MultiplyPoint3x4(hand.CameraLandmarks[k]));
                    Vector3 world = cam.TransformPoint(local);
                    if (smooth)
                    {
                        pose.Joints[k] = pose.Filters[k].Filter(world, (float)dt, smoothMinCutoff, smoothBeta, restart);
                    }
                    else
                    {
                        pose.Joints[k] = world;
                    }
                }

                pose.Tracked = true;
                UpdatePinch(pose, hand, restart);
                // a pinch otherwise reads as a fist, a one or no gesture
                UpdateGesture(pose, pose.Pinching ? HandGesture.Pinch : hand.Gesture, restart);
                pose.Confidence = hand.LandmarkScore;
                pose.DepthCalibrated = hand.StereoDepth > 0f;
                pose.StereoDepth = hand.StereoDepth;
                pose.HandScale = hand.HandScale;
                pose.CaptureTime = frame.Timestamp;
                pose.LastUpdateTime = now;
            }

            Updated?.Invoke(this);
        }
    }

    /// <summary>
    /// One Euro filter (Casiez et al.): low latency when moving fast, strong smoothing when still.
    /// </summary>
    internal sealed class OneEuroVector3
    {
        private Vector3 _value;
        private Vector3 _derivative;
        private bool _initialized;

        public Vector3 Filter(Vector3 input, float dt, float minCutoff, float beta, bool restart)
        {
            if (restart || !_initialized)
            {
                _value = input;
                _derivative = Vector3.zero;
                _initialized = true;
                return input;
            }

            Vector3 rawDerivative = (input - _value) / dt;
            _derivative = Vector3.Lerp(_derivative, rawDerivative, Alpha(1f, dt));
            float cutoff = minCutoff + beta * _derivative.magnitude;
            _value = Vector3.Lerp(_value, input, Alpha(cutoff, dt));
            return _value;
        }

        private static float Alpha(float cutoff, float dt)
        {
            float tau = 1f / (2f * Mathf.PI * cutoff);
            return 1f / (1f + tau / dt);
        }
    }
}
