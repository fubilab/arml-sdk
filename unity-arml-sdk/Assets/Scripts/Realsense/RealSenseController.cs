using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

public class RealSenseController : MonoBehaviour
{
    private const string PLUGIN_NAME = "camera_motion";

    #region Native Plugin Methods
    // Stops pipeline and resets global variables, called OnDestroy
    [DllImport(PLUGIN_NAME)]
    private static extern void cleanupCamera();

    // Sets resolution, dimension and fps of the RGB stream, called on Start
    [DllImport(PLUGIN_NAME)]
    private static extern void colorStreamConfig(int width, int height, int fps);

    // Sets resolution, dimension and fps of the Depth stream, called on Start
    [DllImport(PLUGIN_NAME)]
    private static extern void depthStreamConfig(int width, int height, int fps);

    // Specifies bag file to read prerecorded image data
    [DllImport(PLUGIN_NAME)]
    private static extern void bagFileStreamConfig(string bagFileAddress);

    // Starts camera pipeline
    [DllImport(PLUGIN_NAME)]
    private static extern void initCamera();

    // Starts IMU pipeline
    [DllImport(PLUGIN_NAME)]
    private static extern void initImu();

    // Updates the tracking parameter configuration
    [DllImport(PLUGIN_NAME)]
    private static extern void setParams(systemConfig config);

    // Creates ORB feature extractor
    // see cv::ORB::create
    [DllImport(PLUGIN_NAME)]
    private static extern void createORB(int nfeatures,
                                        float scaleFactor,
                                        int nlevels,
                                        int edgeThreshold,
                                        int firstLevel,
                                        int WTA_K,
                                        int scoreType,
                                        int patchSize,
                                        int fastThreshold
                                    );

    // [DllImport(PLUGIN_NAME)]
    // private static extern void createSIFT(int nfeatures = 0,
    //                                     int nOctaveLayers = 3,
    //                                     double contrastThreshold = 0.04,
    //                                     double edgeThreshold = 10,
    //                                     double sigma = 1.6,
    //                                     bool enable_precise_upscale = false
    //                                 );

    // Waits n frames then waits for the camera to warmup
    // TODO make the wait delay a parameter
    // TODO rename function
    [DllImport(PLUGIN_NAME)]
    private static extern void firstIteration();

    // Process current frame
    // - get camera frame
    // - extract features
    // - look for reference image/object in frame
    // - filter features by crop area
    // - find matches with known keyframe
    // - find matches with prev frame
    // - filter by best matches
    // - reproject color pixels to the depth frame
    // - calculate transform between current and previous frame (or keyframe)
    // - calculate accumulated transform (from world origin)
    // TODO rename
    [DllImport(PLUGIN_NAME)]
    public static extern void findFeatures();

    // Get world position of the camera
    [DllImport(PLUGIN_NAME)]
    private static extern void getTranslationVector(float[] t_f_data);

    public static Vector3 RetrieveTranslationVector()
    {
        float[] t_f_data = new float[3];
        getTranslationVector(t_f_data);
        return new Vector3(t_f_data[0], t_f_data[1], t_f_data[2]);
    }

    // Get world rotation of camera VIO
    [DllImport(PLUGIN_NAME)]
    private static extern void getCameraRotation(float[] R_f_data);

    public static float[] RetrieveCameraQuaternions()
    {
        float[] R_f_data = new float[4];
        getCameraRotation(R_f_data);
        return R_f_data;
    }

    // Get world rotation of camera IMU
    [DllImport(PLUGIN_NAME)]
    private static extern void getCameraOrientation(float[] cameraAngle);

    public static float[] RetrieveCameraOrientation()
    {
        float[] cameraAngle = new float[3];
        getCameraOrientation(cameraAngle);
        return cameraAngle;
    }

    // Resets odometry origin to current frame
    // (e.g., call to fix odometry to a known point after drift)
    [DllImport(PLUGIN_NAME)]
    public static extern void resetOdom();

    // Add current frame to list of known keyframes
    [DllImport(PLUGIN_NAME)]
    public static extern void addKeyframe();

    // Returns true if current frame is a loop closure (recognized keyframe)
    [DllImport(PLUGIN_NAME)]
    public static extern bool isLoop();

    // Returns current frame RGB image
    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr getJpegBuffer(out int bufferSize);

    public static byte[] GetJpegBuffer()
    {
        int bufferSize = 0;
        IntPtr bufferPtr = getJpegBuffer(out bufferSize);

        byte[] jpegBuffer = new byte[bufferSize];
        Marshal.Copy(bufferPtr, jpegBuffer, 0, bufferSize);

        Marshal.FreeCoTaskMem(bufferPtr);

        return jpegBuffer;
    }

    // Returns distance to object at center of frame in meters
    [DllImport(PLUGIN_NAME)]
    private static extern float GetDepthAtCenter();

    // Ignores the features within region of the image - use RealSenseViewer to get coordinates
    [DllImport(PLUGIN_NAME)]
    private static extern void setProjectorZone(int sectionX, int sectionY, int sectionWidth, int sectionHeight);

    // Write keyframe list to a YAML file
    [DllImport(PLUGIN_NAME)]
    private static extern void serializeKeyframeData(string fileName);

    // Load keyframe list to a YAML file
    [DllImport(PLUGIN_NAME)]
    private static extern void deserializeKeyframeData(string fileName);
    #endregion

    public struct systemConfig
    {
        // DO NOT EDIT UNLESS YOU ABSOLUTELY KNOW WHAT YOUŔE DOING  
        // When comparing two features in a match, 
        // this ratio is used to ensure there is enough distance between them to be meaningful.
        // Smaller values will reject more matches
        public float ratioTresh;
        // Feature must be > minDepth (in m) from camera to be considered for matches
        public float minDepth;
        // Feature must be < maxDepth (in m) from camera to be considered for matches
        public float maxDepth;
        // Min number of reprojected features to use current frame in odometry
        // (too low will produce poor or unreliable tracking, too high will drop too many frames)
        public int min3DPoints;
        // Only consider odometry frame if less than this distance from prev frame
        public float maxDistanceF2F;
        // Minimum amount of feature matches between current frame and keyframe to consider it a loop closure
        public int minFeaturesLoopClosure;
        // Minimum rotation to consider the camera in movement
        public float noMovementThresh;
        // Number of frames below noMovementThresh required to consider camera not in movement.
        public int framesNoMovement;
        // Maximum number of good matches considered for the calculation of transformations
        // If number is too big processing may slow down, but if its too small tracking may be unreliable
        public int maxGoodFeatures;
        // Minimum feature matches to consider an object detected.
        // Should be between 30 or 40 (renato test)
        public int minFeaturesFindObject;
    }

    #region Odometry Parameters
    [SerializeField] private bool localizationMode = false;
    [SerializeField] private int colorWidth = 640;
    [SerializeField] private int colorHeight = 480;
    [SerializeField] private int colorFPS = 30;
    [SerializeField] private int depthWidth = 640;
    [SerializeField] private int depthHeight = 480;
    [SerializeField] private int depthFPS = 30;
    [SerializeField] private float ratioTresh = 0.7f;
    [SerializeField] private float minDepth = 0.0f;
    [SerializeField] private float maxDepth = 6.0f;
    [SerializeField] private int min3DPoints = 15;
    [SerializeField] private float maxDistanceF2F = 0.05f;
    [SerializeField] private int minFeaturesLoopClosure = 200;
    [SerializeField] private int framesUntilLoopClosure = 200;
    [SerializeField] private float noMovementThresh = 0.0001f;
    [SerializeField] private int framesNoMovement = 50;
    [SerializeField] private int maxGoodFeatures = 500;
    [SerializeField] private int minFeaturesFindObject = 30;
    [SerializeField] private int xRectangle = 180;
    [SerializeField] private int yRectangle = 65;
    [SerializeField] private int widthRectangle = 325;
    [SerializeField] private int heightRectangle = 200;
    [SerializeField] private string fileName = "keyframeDatabase.yml";
    [SerializeField] private bool useRecord = false;
    [SerializeField] private string bagFileName = "bag1.bag";

    //ORB Parameters
    [Header("ORB Parameters")]
    [SerializeField] private int orbNFeatures = 500;
    [SerializeField] private float orbScaleFactor = 1.2f;
    [SerializeField] private int orbNLevels = 8;
    [SerializeField] private int orbEdgeThreshold = 31;
    [SerializeField] private int orbFirstLevel = 0;
    [SerializeField] private int orbWTA_K = 2;
    [SerializeField] private int orbScoreType = 0;
    [SerializeField] private int orbPatchSize = 31;
    [SerializeField] private int orbFastThreshold = 20;
    #endregion

    private Vector3 realSenseTranslationVector, initialCamPosition;
    private bool isStopped = false;
    private Thread trackingThread;
    private AutoResetEvent resetEvent;
    private bool reset_odom = false;
    private bool add_keyframe_by_hand = false;
    private string filePath;

    private float[] quaternionsCamera;
    private bool loopClosure;
    private Quaternion remappedRealSenseRotation;

    private IMUCameraRotation imuCameraRotation;

    private JobHandle jobHandle;

    //Display Camera Feed
    [SerializeField] bool captureFirstFrame;
    [SerializeField] RawImage feedDisplayImage;

    private void Awake()
    {
#if UNITY_EDITOR
        GetComponent<RealSenseController>().enabled = false;
#endif
        isStopped = false;
    }

    private void Start()
    {
        //Get Camera initial position to apply as offset
        initialCamPosition = transform.localPosition;

        Debug.Log("---------------------------------- INICIO PROGRAMA --------------------------------");
        // Initialize the RealSense camera when the script starts
        string systemPath = Application.persistentDataPath;

        //Records video with camera
        if (useRecord)
        {
            string bagFilePath = systemPath + bagFileName;
            if (File.Exists(bagFilePath))
            {
                // The file exists, you can proceed with your operations on the file.
                Debug.Log("The file exists: " + bagFilePath);
                bagFileStreamConfig(bagFilePath);
            }
            else
            {
                // The file does not exist, handle the case where the file is missing.
                Debug.LogError("The file does not exist: " + bagFilePath);
            }
        }
        else
        {
            colorStreamConfig(colorWidth, colorHeight, colorFPS);
            depthStreamConfig(depthWidth, depthHeight, depthFPS);
        }

        initCamera();
        initImu();

        createORB(orbNFeatures, orbScaleFactor, orbNLevels, orbEdgeThreshold, orbFirstLevel, orbWTA_K, orbScoreType, orbPatchSize, orbFastThreshold);

        systemConfig config = new systemConfig();
        config.ratioTresh = ratioTresh;
        config.minDepth = minDepth;
        config.maxDepth = maxDepth;
        config.min3DPoints = min3DPoints;
        config.maxDistanceF2F = maxDistanceF2F;
        config.minFeaturesLoopClosure = minFeaturesLoopClosure;
        config.noMovementThresh = noMovementThresh;
        config.framesNoMovement = framesNoMovement;
        config.maxGoodFeatures = maxGoodFeatures;
        config.minFeaturesFindObject = minFeaturesFindObject;

        setParams(config);

        setProjectorZone(xRectangle, yRectangle, widthRectangle, heightRectangle);

        filePath = systemPath + "/" + fileName;
        if (!localizationMode)
        {
            FindObjectOfType<TrackingReferenceImageLibrary>().ConvertImagesToByteArrays();
            firstIteration();
        }
        else
        {
            deserializeKeyframeData(filePath);
        }

        //Thread handling
        trackingThread = new Thread(ThreadUpdate);
        trackingThread.Start();
        resetEvent = new AutoResetEvent(false);

        // Used for image tracking reposition
        // imuCameraRotation = GetComponent<IMUCameraRotation?>();

        if (captureFirstFrame)
        {
            Invoke(nameof(SaveFrame), 0.5f);
        }
    }

    private void SaveFrame()
    {
        //Display image feed
        //Texture2D tex = new Texture2D(colorWidth, colorHeight);
        //tex.LoadImage(GetJpegBuffer(out bufferSize));
        //tex.Apply();
        //feedDisplayImage.texture = tex;

        feedDisplayImage.gameObject.SetActive(true);

        //Save into image file
        System.IO.File.WriteAllBytes($"{Application.persistentDataPath}/FirstFrame.jpeg", GetJpegBuffer());

        //Deactivate white image
        feedDisplayImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        //Thread
        resetEvent.Set();

        //Job Testing
        //FindFeaturesJob findFeaturesJob = new FindFeaturesJob()
        //{
        //    realSenseTranslationVector = realSenseTranslationVector
        //};

        //jobHandle = findFeaturesJob.Schedule();

        ////Apply RealSense position to camera, + initialPosition
        Vector3 remappedTranslationVector = new Vector3(-realSenseTranslationVector.x, realSenseTranslationVector.y, -realSenseTranslationVector.z);
        Vector3 rotatedTranslationVector = Quaternion.AngleAxis(0, Vector3.right) * remappedTranslationVector;
        transform.localPosition = initialCamPosition + rotatedTranslationVector;

        //Apply RealSense rotation to camera
        //remappedRealSenseRotation = new Quaternion(quaternionsCamera[0], -quaternionsCamera[1], quaternionsCamera[2], quaternionsCamera[3]);
        //transform.rotation = remappedRealSenseRotation;

        //Reset Odometry
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Resetting Odometry...");
            reset_odom = true;
        }

        //Add keyframe
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("Adding Keyframe...");
            add_keyframe_by_hand = true;
        }

        //Close application - TODO Don't have this here
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        //Loop closure trigger
        if (loopClosure)
        {
            //OnLoopClosure();
        }
    }

    private void LateUpdate()
    {
        ////Apply RealSense position to camera, + initialPosition

        //jobHandle.Complete();

        //Vector3 remappedTranslationVector = new Vector3(-realSenseTranslationVector.x, realSenseTranslationVector.y, -realSenseTranslationVector.z);
        //Vector3 rotatedTranslationVector = Quaternion.AngleAxis(0, Vector3.right) * remappedTranslationVector;
        //transform.localPosition = initialCamPosition + rotatedTranslationVector;
    }

    private void ThreadUpdate()
    {
        while (!isStopped)
        {
            resetEvent.WaitOne();

            findFeatures();
            //float depth = GetDepthAtCenter();

            //Get RealSense translation
            realSenseTranslationVector = RetrieveTranslationVector();

            //Get RealSense rotation
            //quaternionsCamera = RetrieveCameraQuaternions();

            loopClosure = isLoop();

            if (reset_odom == true)
            {
                resetOdom();
                reset_odom = false;
            }

            if (add_keyframe_by_hand == true)
            {
                addKeyframe();
                add_keyframe_by_hand = false;
            }
        }
    }

    // Still trying to make this work TODO
    // private void OnLoopClosure()
    // {
    //     //Send RealSense rotation to IMU script
    //     if (imuCameraRotation)
    //         imuCameraRotation.ReceiveRealSenseLoopClosure(remappedRealSenseRotation);

    //     //Set false so it only runs one frame
    //     loopClosure = false;
    // }

    private void OnDestroy()
    {
#if !UNITY_EDITOR
        CleanUp();
#endif
    }

    private void CleanUp()
    {
        if (!localizationMode)
        {
            serializeKeyframeData(filePath);
        }
        isStopped = true;
        resetEvent.Set();
        trackingThread.Join();
        cleanupCamera();
    }
}

//public struct FindFeaturesJob : IJob
//{
//    public Vector3 realSenseTranslationVector;

//    public void Execute()
//    {
//        //Find features
//        RealSenseController.findFeatures();

//        Debug.Log(RealSenseController.RetrieveTranslationVector());
//        //Get translation vector
//        realSenseTranslationVector = RealSenseController.RetrieveTranslationVector();
//    }
//}
