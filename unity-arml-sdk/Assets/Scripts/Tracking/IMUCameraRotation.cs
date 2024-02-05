using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ANDROID VERSION. Handles the rotation of a camera based on IMU (Inertial Measurement Unit) data.
/// </summary>
public class IMUCameraRotation : MonoBehaviour
{
    private Vector3 initialImuRotation = Vector3.zero;
    private Vector3 camStartEuler;

    private Vector3 correctedImuRotation;

    //[SerializeField] float accelerationThreshold = 15f;
    //[SerializeField] float accelerationAmount = 0.01f;
    //[SerializeField] float updateRate = 120f;

    //[SerializeField] TMPro.TextMeshProUGUI accelerationThresholdText;
    //[SerializeField] TMPro.TextMeshProUGUI accelerationAmountText;
    //[SerializeField] TMPro.TextMeshProUGUI averageFramesText;

    //PostProcessingController postProcessingController;

    //Previous frame rotation
    private Quaternion lastRotation;

    //Averaging stuff   
    //[SerializeField] int averageFrames = 1;
    private int count;
    Queue<Vector3> averagedImuAngularVelocityQueue = new Queue<Vector3>();

    private Vector3 averagedAccelerationVector;

    private void Awake()
    {
#if UNITY_EDITOR
        GetComponent<IMUCameraRotation>().enabled = false;
#endif
    }

    private void Start()
    {
        lastRotation = Quaternion.identity;
        camStartEuler = transform.localRotation.eulerAngles;
    }

    //If this runs on LateUpdate, transform is not updated through network, check for solutions
    private void Update()
    {
        UpdateRotation();
    }

    void UpdateRotation()
    {
        //Get absolute rotation from IMU and remap it for camera
        Quaternion imuRotation = BNO055Sensor.Instance.GetQuaternion();
        Quaternion remappedImuRotation = new Quaternion(imuRotation.y, imuRotation.z, imuRotation.x, imuRotation.w);

        //Always true at the beginning
        if (initialImuRotation == Vector3.zero)
        {
            initialImuRotation = remappedImuRotation.eulerAngles;
        }

        // Substract initial IMU rotation, the absolute rotation becomes origin (0)
        correctedImuRotation = remappedImuRotation.eulerAngles - initialImuRotation;

        //Add Camera start Rotation as offset
        transform.localEulerAngles = correctedImuRotation + camStartEuler;
    }

    public void ReceiveRealSenseLoopClosure(Quaternion loopQuaternion)
    {
        transform.localEulerAngles = loopQuaternion.eulerAngles;
        initialImuRotation = loopQuaternion.eulerAngles;
    }

    #region Old Methods
    //Vector3 GetAngularVelocityVector(Vector3 rotation)
    //{
    //    var deltaRot = Quaternion.Euler(rotation) * Quaternion.Inverse(lastRotation);
    //    var eulerRot = new Vector3(
    //        Mathf.DeltaAngle(0, deltaRot.eulerAngles.x),
    //        Mathf.DeltaAngle(0, deltaRot.eulerAngles.y),
    //        Mathf.DeltaAngle(0, deltaRot.eulerAngles.z));

    //    lastRotation = Quaternion.Euler(rotation);

    //    return eulerRot / Time.deltaTime;
    //}

    //Vector3 GetAveragedAccelerationVector(Vector3 vector)
    //{
    //    averagedImuAngularVelocityQueue.Enqueue(vector);

    //    if (averagedImuAngularVelocityQueue.Count >= averageFrames)
    //    {
    //        var vectorSum = Vector3.zero;

    //        foreach (Vector3 v in averagedImuAngularVelocityQueue)
    //        {
    //            vectorSum += v;
    //        }

    //        averagedAccelerationVector = vectorSum / averagedImuAngularVelocityQueue.Count;
    //        averagedImuAngularVelocityQueue.Clear();
    //    }

    //    return averagedAccelerationVector;
    //}

    //public void SetAccelerationThreshold(float value)
    //{
    //    accelerationThreshold = value;
    //    accelerationThresholdText.text = "Threshold " + value.ToString();
    //}

    //public void SetAccelerationAmount(float value)
    //{
    //    accelerationAmount = value;
    //    accelerationAmountText.text = "Amount " + value.ToString();
    //}

    //public void SetAverageFrames(float value)
    //{
    //    averageFrames = (int)value;
    //    averageFramesText.text = "Frames " + value.ToString();
    //}
    #endregion
}