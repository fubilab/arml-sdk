using System;
using System.Globalization;
using System.IO;
using System.Text;
using SpectacularAI.DepthAI;
using UnityEngine;

namespace ARML.HandTracking
{
    /// <summary>
    /// On screen diagnostics of the VIO and the hand tracking. F1 toggles it.
    /// </summary>
    public class HandTrackingDebugOverlay : MonoBehaviour
    {
        public HandTracker tracker;
        public KeyCode toggleKey = KeyCode.F1;
        public bool visible = true;

        [Header("Camera view")]
        [Tooltip("Show the image of the OAK color camera in the corner of the screen. F2 toggles it.")]
        public bool showCameraView = true;

        public KeyCode cameraViewToggleKey = KeyCode.F2;

        [Tooltip("Width of the camera view as a fraction of the screen width"), Range(0.1f, 0.9f)]
        public float cameraViewWidth = 0.4f;

        [Tooltip("Optional: write a CSV log of the tracked hands (wrist and palm in world coordinates, camera pose)")]
        public string csvPath;

        private StreamWriter _csv;
        private string _vioStatus = "-";

        private readonly StringBuilder _text = new StringBuilder();
        private GUIStyle _style;
        private float _lastLog;

        private Texture2D _cameraTexture;
        private byte[] _cameraPixels;
        private long _cameraSequence = -1;

        private void Start()
        {
            if (tracker == null) tracker = FindFirstObjectByType<HandTracker>();
            if (tracker != null && !string.IsNullOrEmpty(csvPath))
            {
                _csv = new StreamWriter(csvPath, false);
                _csv.WriteLine("unity_time,capture_time,hand,conf,gesture,stereo_calibrated,wrist_x,wrist_y,wrist_z," +
                    "palm_x,palm_y,palm_z,cam_x,cam_y,cam_z,cam_qx,cam_qy,cam_qz,cam_qw,latency_ms,stereo_depth,hand_scale");
                tracker.Updated += LogHands;
            }
        }

        private void OnDestroy()
        {
            if (_cameraTexture != null) Destroy(_cameraTexture);
            if (tracker != null) tracker.Updated -= LogHands;
            if (_csv != null)
            {
                _csv.Dispose();
                _csv = null;
            }
        }

        private void LogHands(HandTracker source)
        {
            if (_csv == null) return;
            Transform cam = source.CameraObject;
            WriteHand(source.Left, cam, source.LatencyMs);
            WriteHand(source.Right, cam, source.LatencyMs);
        }

        private void WriteHand(TrackedHandPose hand, Transform cam, double latencyMs)
        {
            // only hands that were updated by this result
            if (!hand.Tracked || hand.LastUpdateTime != Time.unscaledTime) return;
            var ci = CultureInfo.InvariantCulture;
            Vector3 w = hand.Wrist, p = hand.PalmCenter;
            Vector3 c = cam != null ? cam.position : Vector3.zero;
            UnityEngine.Quaternion q = cam != null ? cam.rotation : UnityEngine.Quaternion.identity;
            _csv.WriteLine(string.Join(",", new[]
            {
                Time.unscaledTime.ToString("F4", ci), hand.CaptureTime.ToString("F4", ci), hand.Hand.ToString(),
                hand.Confidence.ToString("F3", ci), hand.Gesture.ToString(), hand.DepthCalibrated ? "1" : "0",
                w.x.ToString("F4", ci), w.y.ToString("F4", ci), w.z.ToString("F4", ci),
                p.x.ToString("F4", ci), p.y.ToString("F4", ci), p.z.ToString("F4", ci),
                c.x.ToString("F4", ci), c.y.ToString("F4", ci), c.z.ToString("F4", ci),
                q.x.ToString("F5", ci), q.y.ToString("F5", ci), q.z.ToString("F5", ci), q.w.ToString("F5", ci),
                latencyMs.ToString("F1", ci),
                hand.StereoDepth.ToString("F4", ci), hand.HandScale.ToString("F4", ci)
            }));
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) visible = !visible;
            if (Vio.Output != null) _vioStatus = Vio.Output.Status.ToString();
            if (Input.GetKeyDown(cameraViewToggleKey)) showCameraView = !showCameraView;
            if (showCameraView) UpdateCameraView();

            // Same information in the log, so it can be inspected without looking at the screen
            if (tracker != null && Time.unscaledTime - _lastLog > 2f)
            {
                _lastLog = Time.unscaledTime;
                Debug.Log("[HandTracking] " + Describe(false));
            }
        }

        // Copies the newest frame of the OAK color camera into a texture
        private void UpdateCameraView()
        {
            Session session = Vio.ActiveSession;
            if (session == null) return;
            try
            {
                using (ColorFrame frame = session.GetColorFrame())
                {
                    if (frame == null || frame.SequenceNumber == _cameraSequence) return;
                    if (_cameraTexture == null || _cameraTexture.width != frame.Width || _cameraTexture.height != frame.Height)
                    {
                        if (_cameraTexture != null) Destroy(_cameraTexture);
                        _cameraTexture = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
                        _cameraPixels = new byte[frame.Width * frame.Height * 4];
                    }
                    if (!frame.CopyToRgba(_cameraPixels)) return;
                    _cameraTexture.SetPixelData(_cameraPixels, 0);
                    _cameraTexture.Apply(false);
                    _cameraSequence = frame.SequenceNumber;
                }
            }
            catch (EntryPointNotFoundException)
            {
                Debug.LogWarning("[HandTracking] The native plugin cannot provide the camera image, camera view disabled.");
                showCameraView = false;
            }
        }

        private string Describe(bool multiline)
        {
            _text.Length = 0;
            string sep = multiline ? "\n" : " | ";
            _text.Append("VIO: ").Append(_vioStatus)
                .Append(" poses ").Append(Vio.CameraPoses.Count);
            if (tracker == null)
            {
                return _text.Append(sep).Append("no HandTracker").ToString();
            }
            _text.Append(sep).Append("hand tracking ").Append(tracker.Active ? "ON" : "OFF")
                .Append("  ").Append(tracker.ResultsPerSecond.ToString("0.0")).Append(" results/s")
                .Append("  latency ").Append(tracker.LatencyMs.ToString("0")).Append(" ms");
            _text.Append(sep).Append("camera ").Append(tracker.DeviceTemperature.ToString("0")).Append(" C");
            if (tracker.ThrottleLevel > 0) _text.Append("  SLOWED DOWN (level ").Append(tracker.ThrottleLevel).Append(")");
            if (tracker.Stalled) _text.Append("  STALLED - camera not answering");
            AppendHand(tracker.Left, sep);
            AppendHand(tracker.Right, sep);
            return _text.ToString();
        }

        private void AppendHand(TrackedHandPose hand, string sep)
        {
            _text.Append(sep).Append(hand.Hand).Append(": ");
            if (!hand.Tracked)
            {
                _text.Append("-");
                return;
            }
            Vector3 w = hand.Wrist;
            _text.Append(hand.Gesture).Append(" conf ").Append(hand.Confidence.ToString("0.00"))
                .Append(" wrist (").Append(w.x.ToString("0.00")).Append(", ").Append(w.y.ToString("0.00"))
                .Append(", ").Append(w.z.ToString("0.00")).Append(") pinch ")
                .Append((hand.PinchDistance * 100f).ToString("0.0")).Append(" cm")
                .Append(hand.DepthCalibrated ? " [stereo]" : " [size prior]");
        }

        private void OnGUI()
        {
            if (!visible) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = false };
                _style.normal.textColor = Color.white;
            }
            if (showCameraView && _cameraTexture != null)
            {
                float width = Screen.width * cameraViewWidth;
                float height = width * _cameraTexture.height / _cameraTexture.width;
                GUI.DrawTexture(new Rect(Screen.width - width - 12, 12, width, height), _cameraTexture, ScaleMode.StretchToFill, false);
            }
            GUI.Label(new Rect(12, 8, Screen.width - 24, Screen.height - 16), Describe(true), _style);
        }
    }
}
