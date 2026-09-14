using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using SimpleJSON;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

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
        
        // private attributes
        private Color32[] _colorPixel32;
        private GCHandle _colorPixelHandle;
        private IntPtr _colorPixelPtr;
        private Process _handTrackingBridgeProcess;

        private string HandTrackingBridgeDirectory
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "unity_bridge")); }
        }

        private const string HandTrackingBridgeArguments =
            @".\depthai_hand_tracking_unity_bridge.py --use_world_landmarks --xyz --gest --no-preview";

        public override void FinishDevice()
        {
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
            if (!useUnityBridge)
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
                    FileName = "python",
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
        }

        // Prepare Pipeline Configuration and call pipeline init implementation
        protected override bool InitDevice()
        {
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
                    targetLandmarks[i] = wristPosition + Vector3.Scale(relativeLandmark, handLandmarkRemap);
                }
            }
            else
            {
                for (int i = 0; i < landmarkCount; i++)
                {
                    targetLandmarks[i] = Vector3.Scale(modelLandmarks[i], handLandmarkRemap);
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

        // Process results from pipeline
        protected override void ProcessResults()
        {
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

            if (string.IsNullOrEmpty(ubHandTrackingResults)) return;

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
                if (hand0["gesture"] == "FIST")
                {
                    float rotation = (float) hand0["rotation"];
                    rotation *= 0.1f;
                    light.transform.Rotate(Vector3.right, rotation);
                }

            }

            if (hand1 != null)
            {
                if (hand1["gesture"] == "FIST")
                {
                    float rotation = (float) hand1["rotation"];
                    //
                }
            }

            ProcessHand(hand0, landmarks, skeleton, cylinders, connections);
            ProcessHand(hand1, landmarks1, skeleton1, cylinders1, connections1);
        }
    }
}