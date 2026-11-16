using System;
using System.Globalization;
using System.IO;
using System.Text;
using SpectacularAI.DepthAI;
using UnityEngine;

namespace ARML.HandTracking
{
    /// <summary>How the camera image is fitted to the view</summary>
    public enum CameraViewFit
    {
        /// <summary>Covers the whole view, the image is cropped when the aspect ratios differ</summary>
        Fill,
        /// <summary>Shows the whole image, with empty bars when the aspect ratios differ</summary>
        Fit,
        /// <summary>Fills the whole view, the image is distorted when the aspect ratios differ</summary>
        Stretch
    }

    /// <summary>
    /// On screen diagnostics of the VIO and the hand tracking, and the image of the OAK color camera as the
    /// background of the whole view, behind the 3D scene. F1 toggles the text, F2 the camera image.
    /// </summary>
    public class HandTrackingDebugOverlay : MonoBehaviour
    {
        public HandTracker tracker;
        public KeyCode toggleKey = KeyCode.F1;
        public bool visible = true;

        [Header("Camera view")]
        [Tooltip("Show the image of the OAK color camera as the background of the whole view, behind the 3D scene. F2 toggles it.")]
        public bool showCameraView = true;

        public KeyCode cameraViewToggleKey = KeyCode.F2;

        [Tooltip("Fill covers the whole view (crops the image when the aspect ratios differ), Fit shows the whole image, " +
            "Stretch distorts the image to the view.")]
        public CameraViewFit cameraViewFit = CameraViewFit.Fill;

        [Tooltip("The camera that renders the scene. Defaults to the camera of the HandTracker's camera object, or the main camera.")]
        public Camera viewCamera;

        [Header("Gestures")]
        [Tooltip("Show the recognized gesture of each hand: a large readout at the bottom of the screen and a label at each hand.")]
        public bool showGestures = true;

        // same colors as HandSkeletonVisualizer
        private static readonly Color LeftColor = new Color(1f, 0.65f, 0f);
        private static readonly Color RightColor = new Color(0.1f, 0.9f, 0.3f);
        private GUIStyle _captionStyle;
        private GUIStyle _gestureStyle;
        private GUIStyle _handLabelStyle;

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
        private GameObject _backgroundQuad;
        private Material _backgroundMaterial;
        private bool _oakFovKnown;
        private float _oakFovHorizontal;
        private float _oakFovVertical;

        private void Start()
        {
            if (tracker == null) tracker = FindFirstObjectByType<HandTracker>();
            if (tracker != null && !string.IsNullOrEmpty(csvPath))
            {
                _csv = new StreamWriter(csvPath, false);
                _csv.WriteLine("unity_time,capture_time,hand,conf,gesture,stereo_calibrated,wrist_x,wrist_y,wrist_z," +
                    "palm_x,palm_y,palm_z,cam_x,cam_y,cam_z,cam_qx,cam_qy,cam_qz,cam_qw,latency_ms,stereo_depth,hand_scale,raw_gesture");
                tracker.Updated += LogHands;
            }
        }

        private void OnDestroy()
        {
            if (_backgroundQuad != null) Destroy(_backgroundQuad);
            if (_backgroundMaterial != null) Destroy(_backgroundMaterial);
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
                hand.StereoDepth.ToString("F4", ci), hand.HandScale.ToString("F4", ci), hand.RawGesture.ToString()
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
                        if (_backgroundMaterial != null) _backgroundMaterial.mainTexture = _cameraTexture;
                    }
                    if (!frame.CopyToRgba(_cameraPixels)) return;
                    _cameraTexture.SetPixelData(_cameraPixels, 0);
                    _cameraTexture.Apply(false);
                    _cameraSequence = frame.SequenceNumber;
                    if (_backgroundQuad == null) CreateBackground();
                    if (!_oakFovKnown) ReadOakFieldOfView(session);
                }
            }
            catch (EntryPointNotFoundException)
            {
                Debug.LogWarning("[HandTracking] The native plugin cannot provide the camera image, camera view disabled.");
                showCameraView = false;
            }
        }

        // The field of view of the OAK color camera, to compare with the field of view of the Unity camera
        private void ReadOakFieldOfView(Session session)
        {
            if (!session.TryGetHandTrackingInfo(out Vector4 intrinsics, out Vector2Int size)) return;
            if (size.x <= 0 || size.y <= 0 || intrinsics.x <= 0f || intrinsics.y <= 0f) return;
            _oakFovHorizontal = 2f * Mathf.Atan(size.x / (2f * intrinsics.x)) * Mathf.Rad2Deg;
            _oakFovVertical = 2f * Mathf.Atan(size.y / (2f * intrinsics.y)) * Mathf.Rad2Deg;
            _oakFovKnown = true;
        }

        private Camera ResolveViewCamera()
        {
            if (viewCamera != null) return viewCamera;
            Transform cameraObject = tracker != null ? tracker.CameraObject : null;
            Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
            return camera != null ? camera : Camera.main;
        }

        // A quad in front of the camera that covers the view. It is drawn first, so the 3D scene is in front of it.
        private void CreateBackground()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null)
            {
                Debug.LogWarning("[HandTracking] No shader for the camera view, camera view disabled.");
                showCameraView = false;
                return;
            }
            _backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _backgroundQuad.name = "OAK camera feed";
            Destroy(_backgroundQuad.GetComponent<Collider>());
            _backgroundMaterial = new Material(shader) { mainTexture = _cameraTexture };
            _backgroundMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background;
            var meshRenderer = _backgroundQuad.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _backgroundMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            _backgroundQuad.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_backgroundQuad == null) return;
            Camera camera = ResolveViewCamera();
            bool show = showCameraView && _cameraTexture != null && camera != null;
            if (_backgroundQuad.activeSelf != show) _backgroundQuad.SetActive(show);
            if (!show) return;

            if (_backgroundQuad.transform.parent != camera.transform) _backgroundQuad.transform.SetParent(camera.transform, false);

            // size of the view at the distance of the quad
            float distance = Mathf.Min(camera.farClipPlane * 0.5f, 100f);
            float height = 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * camera.aspect;
            var size = new Vector2(width, height);
            if (cameraViewFit != CameraViewFit.Stretch)
            {
                float imageAspect = (float)_cameraTexture.width / _cameraTexture.height;
                bool viewIsWider = camera.aspect > imageAspect;
                bool matchWidth = cameraViewFit == CameraViewFit.Fill ? viewIsWider : !viewIsWider;
                size = matchWidth ? new Vector2(width, width / imageAspect) : new Vector2(height * imageAspect, height);
            }
            Transform quad = _backgroundQuad.transform;
            quad.localPosition = new Vector3(0f, 0f, distance);
            quad.localRotation = UnityEngine.Quaternion.identity;
            quad.localScale = new Vector3(size.x, size.y, 1f);
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
            if (_oakFovKnown)
            {
                // to line up the 3D skeleton with the camera image, set the vertical field of view of the Unity camera
                // to the one of the OAK
                _text.Append(sep).Append("field of view (h x v): OAK ")
                    .Append(_oakFovHorizontal.ToString("0.0")).Append(" x ").Append(_oakFovVertical.ToString("0.0"));
                Camera camera = ResolveViewCamera();
                if (camera != null)
                {
                    _text.Append(" deg, Unity camera ")
                        .Append(Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect).ToString("0.0"))
                        .Append(" x ").Append(camera.fieldOfView.ToString("0.0")).Append(" deg");
                }
            }
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
            _text.Append(hand.Gesture.DisplayName());
            if (hand.RawGesture != hand.Gesture) _text.Append(" (now ").Append(hand.RawGesture.DisplayName()).Append(")");
            _text.Append("  conf ").Append(hand.Confidence.ToString("0.00"))
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
            string text = Describe(true);
            Vector2 textSize = _style.CalcSize(new GUIContent(text));
            if (showCameraView)
            {
                // keep the text readable over the camera image
                Color previous = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(8, 4, textSize.x + 16, textSize.y + 12), Texture2D.whiteTexture);
                GUI.color = previous;
            }
            GUI.Label(new Rect(12, 8, textSize.x + 8, textSize.y + 4), text, _style);

            if (showGestures && tracker != null) DrawGestures();
        }

        private static void DrawBox(Rect rect, float alpha)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        // A readout per hand at the bottom of the screen, and a label above each tracked hand
        private void DrawGestures()
        {
            int big = Mathf.Max(28, Screen.height / 15);
            int small = Mathf.Max(14, Screen.height / 45);
            if (_gestureStyle == null)
            {
                _captionStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                _gestureStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                _handLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            _captionStyle.fontSize = small;
            _gestureStyle.fontSize = big;
            _handLabelStyle.fontSize = Mathf.Max(18, Screen.height / 28);

            float panelWidth = Mathf.Min(Screen.width * 0.32f, big * 12f);
            float panelHeight = big + small + 28f;
            float y = Screen.height - panelHeight - 16f;
            DrawGesturePanel(new Rect(Screen.width * 0.5f - panelWidth - 8f, y, panelWidth, panelHeight), tracker.Left, "LEFT HAND", LeftColor, small, big);
            DrawGesturePanel(new Rect(Screen.width * 0.5f + 8f, y, panelWidth, panelHeight), tracker.Right, "RIGHT HAND", RightColor, small, big);

            Camera camera = ResolveViewCamera();
            if (camera != null)
            {
                DrawHandLabel(tracker.Left, camera, LeftColor);
                DrawHandLabel(tracker.Right, camera, RightColor);
            }
        }

        private void DrawGesturePanel(Rect rect, TrackedHandPose hand, string caption, Color color, int small, int big)
        {
            DrawBox(rect, 0.6f);
            _captionStyle.normal.textColor = color;
            GUI.Label(new Rect(rect.x, rect.y + 6f, rect.width, small + 8f), caption, _captionStyle);

            string text = !hand.Tracked ? "no hand" : (hand.Gesture == HandGesture.None ? "-" : hand.Gesture.DisplayName());
            _gestureStyle.normal.textColor = hand.Tracked ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            GUI.Label(new Rect(rect.x, rect.y + small + 12f, rect.width, big + 14f), text, _gestureStyle);
        }

        private void DrawHandLabel(TrackedHandPose hand, Camera camera, Color color)
        {
            if (!hand.Tracked || hand.Gesture == HandGesture.None) return;
            Vector3 screen = camera.WorldToScreenPoint(hand.PalmCenter);
            if (screen.z <= 0f) return;

            var content = new GUIContent(hand.Gesture.DisplayName());
            Vector2 size = _handLabelStyle.CalcSize(content);
            // above the palm; the screen y axis of the camera points up, the one of the GUI points down
            var rect = new Rect(screen.x - size.x * 0.5f - 8f, Screen.height - screen.y - size.y - 70f, size.x + 16f, size.y + 6f);
            DrawBox(rect, 0.6f);
            _handLabelStyle.normal.textColor = color;
            GUI.Label(rect, content, _handLabelStyle);
        }
    }
}
