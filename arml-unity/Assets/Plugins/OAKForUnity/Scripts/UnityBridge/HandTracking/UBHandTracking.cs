using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using SimpleJSON;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using SpectacularAI.DepthAI;

namespace OAKForUnity
{
    public class UBHandTracking : PredefinedBase
    {
        // For future compatibility between UB and standard C++ plugin
        
        //Lets make our calls from the Plugin
        //[DllImport("depthai-unity", CallingConvention = CallingConvention.Cdecl)]
        /*
        * Pipeline creation based on streams template
        *
        * @param config pipeline configuration 
        * @returns pipeline 
        */
        //private static extern bool InitUBTest (in PipelineConfig config);

        //[DllImport("depthai-unity", CallingConvention = CallingConvention.Cdecl)]
        /*
        * Pipeline results
        *
        * @param frameInfo camera images pointers
        * ................
        * @returns Json with results or information about device availability. 
        */    
        //private static extern IntPtr UBTestResults(out FrameInfo frameInfo, bool getPreview, int width, int height,  ...., int deviceNum);

        [Header("Coordinate Remapping")]
        [Tooltip("Per-axis multiplier for global wrist movement. Set an axis to -1 to invert it.")]
        public Vector3 handPositionRemap = new Vector3(1f, 1f, 1f);
        [Tooltip("Per-axis multiplier for the local hand pose. Set an axis to -1 to mirror it.")]
        public Vector3 handLandmarkRemap = new Vector3(1f, -1f, 1f);

        public bool automaticallyAttachToMainCamera = true;

        [Header("Native SAI VIO")]
        public bool useNativeVio;
        public Vio nativeVio;

        [Header("Native Diagnostics")]
        public bool logNativeDiagnostics = true;
        [Min(1)]
        public int nativeDiagnosticIntervalFrames = 60;

        [Header("Gesture Triggers")]
        public Collider hand0GestureTrigger;
        public Collider hand1GestureTrigger;
        public UBHandGesture hand0TriggerGesture = UBHandGesture.Fist;
        public UBHandGesture hand1TriggerGesture = UBHandGesture.Fist;

        [Header("Results")] 
        public Texture2D colorTexture;
        public string ubHandTrackingResults;
        public GameObject light;
        public int countData;
        [Header("Hand 0")]
        public Vector3[] landmarks;
        public GameObject[] skeleton;
        public GameObject[] cylinders;
        public Vector2[] connections;
        [Header("Hand 1")]
        public Vector3[] landmarks1;
        public GameObject[] skeleton1;
        public GameObject[] cylinders1;
        public Vector2[] connections1;

        private float _oldRotation;
        private UBHandGesture _hand0Gesture;
        private UBHandGesture _hand1Gesture;
        private float _hand0PalmScore;
        private float _hand1PalmScore;
        private float _hand0LandmarkScore;
        private float _hand1LandmarkScore;
        
        // private attributes
        private Color32[] _colorPixel32;
        private GCHandle _colorPixelHandle;
        private IntPtr _colorPixelPtr;
        private Process _handTrackingBridgeProcess;
        private bool _loggedHandTrackingResults;
        private ColorFrame _nativeColorFrame;
        private HandTrackingOutput _nativeHandTrackingOutput;
        private byte[] _nativeInterleavedColorData;
        private long _lastNativeColorSequenceNumber = -1;
        private long _lastNativeHandTrackingSequenceNumber = -1;
        private int _nativePollCount;

        private string HandTrackingBridgeDirectory
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "unity_bridge")); }
        }

#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR
        private const string HandTrackingBridgePython = "python3";
        private const string HandTrackingBridgeArguments =
            "./depthai_hand_tracking_unity_bridge.py --use_world_landmarks --xyz --gest --no-preview";
#else
        private const string HandTrackingBridgePython = "python";
        private const string HandTrackingBridgeArguments =
            @".\depthai_hand_tracking_unity_bridge.py --use_world_landmarks --xyz --gest --no-preview";
#endif

        public override void FinishDevice()
        {
            if (useNativeVio)
            {
                deviceRunning = false;
                StopHandTrackingBridge();
                return;
            }

            try
            {
                base.FinishDevice();
            }
            finally
            {
                StopHandTrackingBridge();
            }
        }

        private void OnDestroy()
        {
            StopHandTrackingBridge();
        }

        private bool StartHandTrackingBridge()
        {
            if (!useUnityBridge || useNativeVio)
            {
                return true;
            }

            if (_handTrackingBridgeProcess != null)
            {
                if (!_handTrackingBridgeProcess.HasExited)
                {
                    return true;
                }

                _handTrackingBridgeProcess.Dispose();
                _handTrackingBridgeProcess = null;
            }

            try
            {
                _handTrackingBridgeProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = HandTrackingBridgePython,
                    Arguments = HandTrackingBridgeArguments,
                    WorkingDirectory = HandTrackingBridgeDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (_handTrackingBridgeProcess == null)
                {
                    Debug.LogError("Could not start the hand tracking bridge process.");
                    return false;
                }

                return true;
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                Debug.LogError("Could not start the hand tracking bridge process: " + exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError("Could not start the hand tracking bridge process: " + exception.Message);
            }
            catch (System.IO.DirectoryNotFoundException exception)
            {
                Debug.LogError("Could not start the hand tracking bridge process: " + exception.Message);
            }

            return false;
        }

        private void StopHandTrackingBridge()
        {
            if (_handTrackingBridgeProcess == null)
            {
                return;
            }

            try
            {
                if (!_handTrackingBridgeProcess.HasExited)
                {
                    _handTrackingBridgeProcess.Kill();
                    _handTrackingBridgeProcess.WaitForExit(2000);
                }
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError("Could not stop the hand tracking bridge process: " + exception.Message);
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                Debug.LogError("Could not stop the hand tracking bridge process: " + exception.Message);
            }
            finally
            {
                _handTrackingBridgeProcess.Dispose();
                _handTrackingBridgeProcess = null;
            }
        }

        public override void ConnectDevice()
        {
            LogNativeDiagnostics(
                "ConnectDevice called. configuredProcessMode=" + processMode +
                ", useNativeVio=" + useNativeVio +
                ", nativeVioAssigned=" + (nativeVio != null));
            if (useNativeVio)
            {
                processMode = ProcessMode.UnityThread;
                if (!deviceRunning)
                {
                    deviceRunning = InitDevice();
                }

                return;
            }

            base.ConnectDevice();
        }

        // Init textures. Each PredefinedBase implementation handles textures. Decoupled from external viz (Canvas, VFX, ...)
        void InitTexture()
        {
            colorTexture = new Texture2D(300, 300, TextureFormat.ARGB32, false);
            _colorPixel32 = colorTexture.GetPixels32();
            //Pin pixel32 array
            _colorPixelHandle = GCHandle.Alloc(_colorPixel32, GCHandleType.Pinned);
            //Get the pinned address
            _colorPixelPtr = _colorPixelHandle.AddrOfPinnedObject();
        }

        // Start. Init textures and frameInfo
        void Start()
        {
            LogNativeDiagnostics(
                "UB component Start. useNativeVio=" + useNativeVio +
                ", useUnityBridge=" + useUnityBridge);
            // Init dataPath to load body pose NN model
            _dataPath = Application.dataPath;
            StartHandTrackingBridge();
            
            InitTexture();

            // Init FrameInfo. Only need it in case memcpy data ptr on plugin lib.
            frameInfo.colorPreviewData = _colorPixelPtr;

            countData = -1;
            _oldRotation = 0f;
            landmarks = new Vector3[21];
            landmarks1 = new Vector3[21];
            _hand0Gesture = UBHandGesture.None;
            _hand1Gesture = UBHandGesture.None;
            UpdateGestureTriggers();
            
            if(Camera.main != null && automaticallyAttachToMainCamera)
                this.transform.parent = Camera.main.transform;

            if (useNativeVio)
            {
                LogNativeDiagnostics("UB component Start completed. Starting native Vio connection.");
                ConnectDevice();
            }
        }

        // Prepare Pipeline Configuration and call pipeline init implementation
        protected override bool InitDevice()
        {
            LogNativeDiagnostics("InitDevice entered.");
            if (useNativeVio)
            {
                if (nativeVio == null)
                {
                    nativeVio = FindFirstObjectByType<Vio>();
                }

                if (nativeVio == null)
                {
                    Debug.LogError("Native SAI VIO hand tracking requires a Vio component.");
                    return false;
                }

                try
                {
                    if (nativeVio.IsSessionStarted)
                    {
                        if (!nativeVio.UseColor || !nativeVio.EnableHandTracking)
                        {
                            Debug.LogError(
                                "The native Vio session started before UBHandTracking and does not have color and hand tracking enabled.");
                            return false;
                        }
                    }
                    else
                    {
                        nativeVio.UseColor = true;
                        nativeVio.EnableHandTracking = true;
                        nativeVio.StartSession();
                    }

                    deviceRunning = true;
                    LogNativeDiagnostics(
                        "Native Vio ready. " +
                        "sessionStarted=" + nativeVio.IsSessionStarted +
                        ", useColor=" + nativeVio.UseColor +
                        ", enableHandTracking=" + nativeVio.EnableHandTracking +
                        ", processMode=" + processMode);
                    return true;
                }
                catch (Exception exception)
                {
                    Debug.LogError("Could not initialize native SAI VIO hand tracking: " + exception.Message);
                    deviceRunning = false;
                    return false;
                }
            }

            // For future compatibility between UB and standard C++ plugin

            // Color camera
            /*config.colorCameraFPS = cameraFPS;
            config.colorCameraResolution = (int) rgbResolution;
            config.colorCameraInterleaved = Interleaved;
            config.colorCameraColorOrder = (int) ColorOrderV;
            ....
            */
            
            deviceRunning = false;
            if (useUnityBridge)
            {
                deviceRunning = tcpClientBehaviour.InitUB();
            }
            /*else
            {
                // Plugin lib init pipeline implementation
                deviceRunning = InitUBTest(config);
            }*/

            // Check if was possible to init device with pipeline. Base class handles replay data if possible.
            if (!deviceRunning)
                Debug.LogError(
                    "Was not possible to initialize UB Hand Tracking. Check you have available devices on OAK For Unity -> Device Manager and check you setup correct deviceId if you setup one.");

            return deviceRunning;
        }

        // Get results from pipeline
        protected override void GetResults()
        {
            if (useNativeVio)
            {
                if (nativeVio == null)
                {
                    return;
                }

                _nativePollCount++;
                bool logPoll = ShouldLogNativePoll();
                if (logPoll)
                {
                    Debug.Log("[UBHandTracking] Native poll begin #" + _nativePollCount);
                }

                float colorPollStart = Time.realtimeSinceStartup;
                _nativeColorFrame = nativeVio.GetLatestColorFrame();
                if (logPoll || (_nativeColorFrame != null &&
                    _nativeColorFrame.SequenceNumber != _lastNativeColorSequenceNumber))
                {
                    Debug.Log(
                        "[UBHandTracking] Color poll completed in " +
                        ((Time.realtimeSinceStartup - colorPollStart) * 1000f).ToString("0") +
                        " ms; frame=" + DescribeNativeColorFrame(_nativeColorFrame));
                }

                if (logPoll)
                {
                    Debug.Log("[UBHandTracking] Hand poll begin #" + _nativePollCount);
                }

                float handPollStart = Time.realtimeSinceStartup;
                HandTrackingOutput output = nativeVio.GetLatestHandTrackingOutput();
                if (logPoll || output != null &&
                    output.SequenceNumber != _lastNativeHandTrackingSequenceNumber)
                {
                    Debug.Log(
                        "[UBHandTracking] Hand poll completed in " +
                        ((Time.realtimeSinceStartup - handPollStart) * 1000f).ToString("0") +
                        " ms; output=" + DescribeNativeHandOutput(output));
                }

                if (output != null && output.SequenceNumber != _lastNativeHandTrackingSequenceNumber)
                {
                    _nativeHandTrackingOutput = output;
                    _lastNativeHandTrackingSequenceNumber = output.SequenceNumber;
                }

                return;
            }

            // if not doing replay
            if (!device.replayResults)
            {
                if (useUnityBridge)
                {
                    ubHandTrackingResults = tcpClientBehaviour.GetResults(out colorTexture);
                }
                /*else
                {
                    // Plugin lib pipeline results implementation
                    results = Marshal.PtrToStringAnsi(UBTestResults(out frameInfo, GETPreview, 300, 300,
                        UseDepth, ..., retrieveSystemInformation,
                        useIMU,
                        useSpatialLocator, (int) device.deviceNum));
                }*/
            }
            // if replay read results from file
            else
            {
                ubHandTrackingResults = device.results;
            }
        }

        public UBHandGesture GetHandGesture(int handIndex)
        {
            if (handIndex == 0)
            {
                return _hand0Gesture;
            }

            if (handIndex == 1)
            {
                return _hand1Gesture;
            }

            return UBHandGesture.None;
        }

        public bool IsHandGesture(int handIndex, UBHandGesture gesture)
        {
            return gesture != UBHandGesture.None && GetHandGesture(handIndex) == gesture;
        }

        public float GetHandPalmScore(int handIndex)
        {
            if (handIndex == 0)
            {
                return _hand0PalmScore;
            }

            if (handIndex == 1)
            {
                return _hand1PalmScore;
            }

            return 0f;
        }

        public float GetHandLandmarkScore(int handIndex)
        {
            if (handIndex == 0)
            {
                return _hand0LandmarkScore;
            }

            if (handIndex == 1)
            {
                return _hand1LandmarkScore;
            }

            return 0f;
        }

        public bool TryGetGestureTriggerHand(
            Collider trigger,
            UBHandGesture gesture,
            out int handIndex)
        {
            handIndex = -1;
            if (gesture == UBHandGesture.None)
            {
                return false;
            }

            if (trigger == hand0GestureTrigger && IsHandGesture(0, gesture))
            {
                handIndex = 0;
                return true;
            }

            if (trigger == hand1GestureTrigger && IsHandGesture(1, gesture))
            {
                handIndex = 1;
                return true;
            }

            return false;
        }

        public Vector3[] GetHandLandmarks(int handIndex)
        {
            if (handIndex == 0)
            {
                return landmarks;
            }

            if (handIndex == 1)
            {
                return landmarks1;
            }

            return null;
        }

        public bool TryGetHandPosition(int handIndex, out Vector3 position)
        {
            position = Vector3.zero;
            Vector3[] handLandmarks = GetHandLandmarks(handIndex);
            if (handLandmarks == null || handLandmarks.Length == 0)
            {
                return false;
            }

            position = handLandmarks[0];
            return position != Vector3.zero;
        }
        void PlaceConnection(GameObject sp1, GameObject sp2, GameObject cyl)
        {
            Vector3 v3Start = sp1.transform.position;
            Vector3 v3End = sp2.transform.position;
     
            cyl.transform.position = (v3End - v3Start)/2.0f + v3Start;
     
            Vector3 v3T = cyl.transform.localScale; 
            v3T.y = (v3End - v3Start).magnitude/2; 
        
            cyl.transform.localScale = v3T;
     
            cyl.transform.rotation = Quaternion.FromToRotation(Vector3.up, v3End - v3Start);
        }

        private bool TryGetWristPosition(JSONNode hand, out Vector3 wristPosition)
        {
            wristPosition = Vector3.zero;
            if (hand == null)
            {
                return false;
            }

            var xyz = hand["xyz"];
            if (xyz == null || xyz.Count < 3)
            {
                return false;
            }

            wristPosition = new Vector3(
                (float)xyz[0] / 1000.0f,
                (float)xyz[1] / 1000.0f,
                (float)xyz[2] / 1000.0f);
            wristPosition = Vector3.Scale(wristPosition, handPositionRemap);
            return true;
        }

        private Vector3 TrackingToWorld(Vector3 trackingPosition)
        {
            return transform.TransformPoint(trackingPosition);
        }

        private void ProcessHand(
            JSONNode hand,
            Vector3[] targetLandmarks,
            GameObject[] targetSkeleton,
            GameObject[] targetCylinders,
            Vector2[] targetConnections)
        {
            for (int i = 0; i < targetLandmarks.Length; i++)
            {
                targetLandmarks[i] = Vector3.zero;
                targetSkeleton[i].SetActive(false);
                targetCylinders[i].SetActive(false);
            }

            if (hand == null)
            {
                return;
            }

            var arr = hand["world_landmarks"];
            if (arr == null || arr.Count == 0)
            {
                return;
            }

            var modelLandmarks = new Vector3[targetLandmarks.Length];
            int landmarkCount = Mathf.Min(arr.Count, modelLandmarks.Length);
            for (int i = 0; i < landmarkCount; i++)
            {
                JSONNode landmark = arr[i];
                modelLandmarks[i] = new Vector3(
                    (float)landmark[0],
                    (float)landmark[1],
                    (float)landmark[2]);
            }

            Vector3 wristPosition;
            if (TryGetWristPosition(hand, out wristPosition))
            {
                float rotation = (float)hand["rotation"];
                float sinRotation = Mathf.Sin(rotation);
                float cosRotation = Mathf.Cos(rotation);
                Vector3 modelWrist = modelLandmarks[0];
                Vector3 rotatedWrist = new Vector3(
                    modelWrist.x * cosRotation - modelWrist.y * sinRotation,
                    modelWrist.x * sinRotation + modelWrist.y * cosRotation,
                    modelWrist.z);

                for (int i = 0; i < landmarkCount; i++)
                {
                    Vector3 modelLandmark = modelLandmarks[i];
                    Vector3 rotatedLandmark = new Vector3(
                        modelLandmark.x * cosRotation - modelLandmark.y * sinRotation,
                        modelLandmark.x * sinRotation + modelLandmark.y * cosRotation,
                        modelLandmark.z);
                    Vector3 relativeLandmark = rotatedLandmark - rotatedWrist;
                    targetLandmarks[i] = TrackingToWorld(
                        wristPosition + Vector3.Scale(relativeLandmark, handLandmarkRemap));
                }
            }
            else
            {
                for (int i = 0; i < landmarkCount; i++)
                {
                    targetLandmarks[i] = TrackingToWorld(
                        Vector3.Scale(modelLandmarks[i], handLandmarkRemap));
                }
            }

            bool hasLandmarks = false;
            for (int i = 0; i < targetLandmarks.Length; i++)
            {
                if (targetLandmarks[i] != Vector3.zero)
                {
                    hasLandmarks = true;
                    targetSkeleton[i].SetActive(true);
                    targetSkeleton[i].transform.position = targetLandmarks[i];
                }
            }

            if (!hasLandmarks)
            {
                return;
            }

            for (int i = 0; i < targetConnections.Length; i++)
            {
                int start = (int)targetConnections[i].x;
                int end = (int)targetConnections[i].y;
                if (targetLandmarks[start] != Vector3.zero && targetLandmarks[end] != Vector3.zero)
                {
                    targetCylinders[i].SetActive(true);
                    PlaceConnection(targetSkeleton[start], targetSkeleton[end], targetCylinders[i]);
                }
            }
        }

        private void ProcessHand(
            HandTrackingDetection hand,
            Vector3[] targetLandmarks,
            GameObject[] targetSkeleton,
            GameObject[] targetCylinders,
            Vector2[] targetConnections)
        {
            for (int i = 0; i < targetLandmarks.Length; i++)
            {
                targetLandmarks[i] = Vector3.zero;
                targetSkeleton[i].SetActive(false);
                targetCylinders[i].SetActive(false);
            }

            if (hand == null || hand.WorldLandmarks == null || hand.WorldLandmarks.Length < 3)
            {
                return;
            }

            int landmarkCount = Mathf.Min(targetLandmarks.Length, hand.WorldLandmarks.Length / 3);
            for (int i = 0; i < landmarkCount; i++)
            {
                Vector3 modelLandmark = new Vector3(
                    hand.WorldLandmarks[i * 3],
                    hand.WorldLandmarks[i * 3 + 1],
                    hand.WorldLandmarks[i * 3 + 2]);
                targetLandmarks[i] = TrackingToWorld(
                    Vector3.Scale(modelLandmark, handLandmarkRemap));
            }

            bool hasLandmarks = false;
            for (int i = 0; i < targetLandmarks.Length; i++)
            {
                if (targetLandmarks[i] != Vector3.zero)
                {
                    hasLandmarks = true;
                    targetSkeleton[i].SetActive(true);
                    targetSkeleton[i].transform.position = targetLandmarks[i];
                }
            }

            if (!hasLandmarks)
            {
                return;
            }

            for (int i = 0; i < targetConnections.Length; i++)
            {
                int start = (int)targetConnections[i].x;
                int end = (int)targetConnections[i].y;
                if (targetLandmarks[start] != Vector3.zero && targetLandmarks[end] != Vector3.zero)
                {
                    targetCylinders[i].SetActive(true);
                    PlaceConnection(targetSkeleton[start], targetSkeleton[end], targetCylinders[i]);
                }
            }
        }

        private void UpdateNativeColorTexture(ColorFrame frame)
        {
            if (frame == null || frame.Width <= 0 || frame.Height <= 0)
            {
                return;
            }

            int pixelCount = checked(frame.Width * frame.Height);
            if (frame.Data == null || frame.Data.Length < pixelCount * 3)
            {
                return;
            }

            if (colorTexture == null || colorTexture.width != frame.Width || colorTexture.height != frame.Height)
            {
                if (colorTexture != null)
                {
                    Destroy(colorTexture);
                }

                colorTexture = new Texture2D(frame.Width, frame.Height, TextureFormat.RGB24, false);
                colorTexture.wrapMode = TextureWrapMode.Clamp;
                colorTexture.filterMode = FilterMode.Bilinear;
            }

            int dataSize = pixelCount * 3;
            if (_nativeInterleavedColorData == null || _nativeInterleavedColorData.Length != dataSize)
            {
                _nativeInterleavedColorData = new byte[dataSize];
            }

            for (int pixelIndex = 0; pixelIndex < pixelCount; pixelIndex++)
            {
                _nativeInterleavedColorData[pixelIndex * 3] = frame.Data[pixelIndex];
                _nativeInterleavedColorData[pixelIndex * 3 + 1] = frame.Data[pixelCount + pixelIndex];
                _nativeInterleavedColorData[pixelIndex * 3 + 2] = frame.Data[pixelCount * 2 + pixelIndex];
            }

            colorTexture.LoadRawTextureData(_nativeInterleavedColorData);
            colorTexture.Apply(false, false);
        }

        private HandTrackingDetection GetNativeDetection(int handIndex)
        {
            if (_nativeHandTrackingOutput == null || _nativeHandTrackingOutput.Detections == null)
            {
                return null;
            }

            int rightHandIndex = -1;
            int leftHandIndex = -1;
            for (int index = 0; index < _nativeHandTrackingOutput.Detections.Length; index++)
            {
                if (_nativeHandTrackingOutput.Detections[index].Handedness >= 0.5f)
                {
                    if (rightHandIndex < 0)
                    {
                        rightHandIndex = index;
                    }
                }
                else if (leftHandIndex < 0)
                {
                    leftHandIndex = index;
                }
            }

            int detectionIndex;
            if (handIndex == 0)
            {
                detectionIndex = rightHandIndex >= 0
                    ? rightHandIndex
                    : (_nativeHandTrackingOutput.Detections.Length > 0 ? 0 : -1);
            }
            else
            {
                detectionIndex = leftHandIndex >= 0
                    ? leftHandIndex
                    : (_nativeHandTrackingOutput.Detections.Length > 1 ?
                        (rightHandIndex == 0 ? 1 : 0) : -1);
            }
            if (detectionIndex < 0 || detectionIndex >= _nativeHandTrackingOutput.Detections.Length)
            {
                return null;
            }

            return _nativeHandTrackingOutput.Detections[detectionIndex];
        }

        private static UBHandGesture ParseNativeGesture(int gesture)
        {
            return gesture >= (int)UBHandGesture.None && gesture <= (int)UBHandGesture.Peace
                ? (UBHandGesture)gesture
                : UBHandGesture.None;
        }

        private void ProcessNativeResults()
        {
            if (_nativeColorFrame != null &&
                _nativeColorFrame.SequenceNumber != _lastNativeColorSequenceNumber)
            {
                UpdateNativeColorTexture(_nativeColorFrame);
                _lastNativeColorSequenceNumber = _nativeColorFrame.SequenceNumber;
            }

            HandTrackingDetection hand0 = GetNativeDetection(0);
            HandTrackingDetection hand1 = GetNativeDetection(1);
            _hand0PalmScore = hand0 == null ? 0f : hand0.Score;
            _hand1PalmScore = hand1 == null ? 0f : hand1.Score;
            _hand0LandmarkScore = hand0 == null ? 0f : hand0.LandmarkScore;
            _hand1LandmarkScore = hand1 == null ? 0f : hand1.LandmarkScore;
            _hand0Gesture = hand0 == null ? UBHandGesture.None : ParseNativeGesture(hand0.Gesture);
            _hand1Gesture = hand1 == null ? UBHandGesture.None : ParseNativeGesture(hand1.Gesture);
            ubHandTrackingResults = string.Format(
                "Native SAI hand tracking: {0} hand(s)",
                _nativeHandTrackingOutput == null || _nativeHandTrackingOutput.Detections == null
                    ? 0
                    : _nativeHandTrackingOutput.Detections.Length);
            UpdateGestureTriggers();

            ProcessHand(hand0, landmarks, skeleton, cylinders, connections);
            ProcessHand(hand1, landmarks1, skeleton1, cylinders1, connections1);
        }

        private bool ShouldLogNativePoll()
        {
            return logNativeDiagnostics &&
                (_nativePollCount == 1 ||
                 _nativePollCount % Mathf.Max(1, nativeDiagnosticIntervalFrames) == 0);
        }

        private void LogNativeDiagnostics(string message)
        {
            if (logNativeDiagnostics)
            {
                Debug.Log("[UBHandTracking] " + message);
            }
        }

        private static string DescribeNativeColorFrame(ColorFrame frame)
        {
            if (frame == null)
            {
                return "null";
            }

            return string.Format(
                "seq={0}, size={1}x{2}, bytes={3}",
                frame.SequenceNumber,
                frame.Width,
                frame.Height,
                frame.Data == null ? 0 : frame.Data.Length);
        }

        private static string DescribeNativeHandOutput(HandTrackingOutput output)
        {
            if (output == null)
            {
                return "null";
            }

            string description = string.Format(
                "seq={0}, detections={1}",
                output.SequenceNumber,
                output.Detections == null ? 0 : output.Detections.Length);
            if (output.Detections == null)
            {
                return description;
            }

            for (int index = 0; index < output.Detections.Length; index++)
            {
                HandTrackingDetection detection = output.Detections[index];
                description += string.Format(
                    " [#{0} palm={1:0.000} landmark={2:0.000} handedness={3:0.000} gesture={4}]",
                    index,
                    detection.Score,
                    detection.LandmarkScore,
                    detection.Handedness,
                    detection.Gesture);
            }

            return description;
        }

        // Process results from pipeline
        protected override void ProcessResults()
        {
            if (useNativeVio)
            {
                _hand0Gesture = UBHandGesture.None;
                _hand1Gesture = UBHandGesture.None;
                _hand0PalmScore = 0f;
                _hand1PalmScore = 0f;
                _hand0LandmarkScore = 0f;
                _hand1LandmarkScore = 0f;
                ProcessNativeResults();
                return;
            }

            // If not replaying data
            if (!device.replayResults)
            {
            }
            // if replaying data
            else
            {
                // Apply textures but get them from unity device implementation
                for (int i = 0; i < device.textureNames.Count; i++)
                {
                    if (device.textureNames[i] == "color")
                    {
                        colorTexture.SetPixels32(device.textures[i].GetPixels32());
                        colorTexture.Apply();
                    }
                }
            }

            _hand0Gesture = UBHandGesture.None;
            _hand1Gesture = UBHandGesture.None;
            _hand0PalmScore = 0f;
            _hand1PalmScore = 0f;
            _hand0LandmarkScore = 0f;
            _hand1LandmarkScore = 0f;
            UpdateGestureTriggers();
            if (string.IsNullOrEmpty(ubHandTrackingResults))
            {
                ProcessHand((JSONNode)null, landmarks, skeleton, cylinders, connections);
                ProcessHand((JSONNode)null, landmarks1, skeleton1, cylinders1, connections1);
                return;
            }

            if (!_loggedHandTrackingResults)
            {
                Debug.Log("Unity Bridge hand tracking JSON: " + ubHandTrackingResults);
                _loggedHandTrackingResults = true;
            }

            // EXAMPLE HOW TO PARSE INFO
            var json = JSON.Parse(ubHandTrackingResults);
            var arr2 = json["res2"]["arr1"];

            if (countData == -1)
            {
                countData = (int)arr2[0];
            }
            else
            {
                if (countData+2 < arr2[0]) Debug.LogError("MISSING DATA "+countData+ " "+arr2[0]);
                countData = arr2[0];
            }
            
            var hand0 = json["hand_0"];
            var hand1 = json["hand_1"];

            if (hand0 != null && hand0["label"] == "left")
            {
                hand0 = json["hand_1"];
                hand1 = json["hand_0"];
            }

            if (hand0 != null)
            {
                _hand0PalmScore = GetHandScore(hand0, "pd_score");
                _hand0LandmarkScore = GetHandScore(hand0, "lm_score");
                if (hand0["gesture"] == "FIST")
                {
                    float rotation = (float) hand0["rotation"];
                    rotation *= 0.1f;
                }

            }

            if (hand1 != null)
            {
                _hand1PalmScore = GetHandScore(hand1, "pd_score");
                _hand1LandmarkScore = GetHandScore(hand1, "lm_score");
                if (hand1["gesture"] == "FIST")
                {
                    float rotation = (float) hand1["rotation"];
                    //
                }
            }

            _hand0Gesture = ParseGesture(hand0);
            _hand1Gesture = ParseGesture(hand1);
            UpdateGestureTriggers();

            ProcessHand(hand0, landmarks, skeleton, cylinders, connections);
            ProcessHand(hand1, landmarks1, skeleton1, cylinders1, connections1);
        }

        private float GetHandScore(JSONNode hand, string scoreName)
        {
            if (hand == null)
            {
                return 0f;
            }

            JSONNode score = hand[scoreName];
            return score == null || score.IsNull ? 0f : score.AsFloat;
        }

        private UBHandGesture ParseGesture(JSONNode hand)
        {
            if (hand == null)
            {
                return UBHandGesture.None;
            }

            switch (hand["gesture"].Value)
            {
                case "ONE":
                    return UBHandGesture.One;
                case "TWO":
                    return UBHandGesture.Two;
                case "THREE":
                    return UBHandGesture.Three;
                case "FOUR":
                    return UBHandGesture.Four;
                case "FIVE":
                    return UBHandGesture.Five;
                case "FIST":
                    return UBHandGesture.Fist;
                case "OK":
                    return UBHandGesture.Ok;
                case "PEACE":
                    return UBHandGesture.Peace;
                default:
                    return UBHandGesture.None;
            }
        }

        private void UpdateGestureTriggers()
        {
            if (hand0GestureTrigger != null)
            {
                hand0GestureTrigger.enabled =
                    IsHandGesture(0, hand0TriggerGesture);
            }

            if (hand1GestureTrigger != null)
            {
                hand1GestureTrigger.enabled =
                    IsHandGesture(1, hand1TriggerGesture);
            }
        }
    }
}