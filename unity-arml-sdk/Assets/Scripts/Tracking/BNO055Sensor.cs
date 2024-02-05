using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class BNO055Sensor : MonoBehaviour
{
    private const string PLUGIN_NAME = "bno055";

    // Native Plugin Functions
    [DllImport(PLUGIN_NAME)]
    private static extern bool setMode(string device_path, int device_address, byte register_address, byte mode);

    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr getAccelerometer(string device_path, int device_address);

    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr getMagnetometer(string device_path, int device_address);

    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr getGyroscope(string device_path, int device_address);

    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr getEuler(string device_path, int device_address);

    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr getQuaternion(string device_path, int device_address);

    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr getLinearAcceleration(string device_path, int device_address);

    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr getGravity(string device_path, int device_address);

    [DllImport(PLUGIN_NAME)]
    private static extern IntPtr getSystemStatus(string device_path, int device_address);

    [DllImport(PLUGIN_NAME)]
    private static extern void closeDevice();

    // Public variables to set device path and address
    private string devicePath = "/dev/i2c-5";
    private int deviceAddress = 0x28;
    private byte registerAddress = 0x3d;
    private byte mode = 0x08;

    public static BNO055Sensor Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // Function to set the mode of the BNO055 sensor
    public bool SetMode(byte registerAddress, byte mode)
    {
        return setMode(devicePath, deviceAddress, registerAddress, mode);
    }

    // Function to get the accelerometer data from the BNO055 sensor 
    public Vector3 GetAccelerometer()
    {
        IntPtr accelDataPtr = getAccelerometer(devicePath, deviceAddress);
        float[] accelData = new float[3];
        Marshal.Copy(accelDataPtr, accelData, 0, 3);

        // Convert the data to Unity's Vector3 format (order: x, y, z)
        return new Vector3(accelData[0], accelData[1], accelData[2]);
    }

    // Function to get the magnetometer data from the BNO055 sensor 
    public Vector3 GetMagnetometer()
    {
        IntPtr magDataPtr = getMagnetometer(devicePath, deviceAddress);
        float[] magData = new float[3];
        Marshal.Copy(magDataPtr, magData, 0, 3);

        // Convert the data to Unity's Vector3 format (order: x, y, z)
        return new Vector3(magData[0], magData[1], magData[2]);
    }

    // Function to get the gyroscope data from the BNO055 sensor
    public Vector3 GetGyroscope()
    {
        IntPtr gyroDataPtr = getGyroscope(devicePath, deviceAddress);
        float[] gyroData = new float[3];
        Marshal.Copy(gyroDataPtr, gyroData, 0, 3);

        // Convert the data to Unity's Vector3 format (order: x, y, z)
        return new Vector3(gyroData[0], gyroData[1], gyroData[2]);
    }

    // Function to get the euler angles data from the BNO055 sensor
    public Vector3 GetEulerAngles()
    {
        IntPtr eulerDataPtr = getEuler(devicePath, deviceAddress);
        float[] eulerData = new float[3];
        Marshal.Copy(eulerDataPtr, eulerData, 0, 3);

        // Convert the data to Unity's Vector3 format (order: Roll, Pitch, Yaw)
        return new Vector3(eulerData[0], eulerData[1], eulerData[2]);
    }

    // Function to get the quaternion data from the BNO055 sensor
    public Quaternion GetQuaternion()
    {
        IntPtr quatDataPtr = getQuaternion(devicePath, deviceAddress);
        float[] quatData = new float[4];
        Marshal.Copy(quatDataPtr, quatData, 0, 4);

        // Create a Unity Quaternion using the BNO055 data (order: x, y, z, w)
        return new Quaternion(quatData[1], quatData[2], quatData[3], quatData[0]);
    }

    // Function to get the linear acceleration data from the BNO055 sensor
    public Vector3 GetLinearAcceleration()
    {
        IntPtr linearAccelDataPtr = getLinearAcceleration(devicePath, deviceAddress);
        float[] linearAccelData = new float[3];
        Marshal.Copy(linearAccelDataPtr, linearAccelData, 0, 3);

        // Convert the data to Unity's Vector3 format (order: x, y, z)
        return new Vector3(linearAccelData[0], linearAccelData[1], linearAccelData[2]);
    }

    // Function to get the gravity data from the BNO055 sensor
    public Vector3 GetGravity()
    {
        IntPtr gravityDataPtr = getGravity(devicePath, deviceAddress);
        float[] gravityData = new float[3];
        Marshal.Copy(gravityDataPtr, gravityData, 0, 3);

        // Convert the data to Unity's Vector3 format
        return new Vector3(gravityData[0], gravityData[1], gravityData[2]);
    }

    // Function to get the system status data from the BNO055 sensor
    public int[] GetSystemStatus()
    {
        IntPtr statusDataPtr = getSystemStatus(devicePath, deviceAddress);
        int[] statusData = new int[3];
        Marshal.Copy(statusDataPtr, statusData, 0, 3);

        return statusData;
    }

    private void Start()
    {
#if UNITY_EDITOR
        return;
#endif
        SetModeCheck();
    }

    void SetModeCheck()
    {
        // Set mode of the sensor
        bool result = SetMode(registerAddress, mode);
        if (result)
        {
            Debug.Log("setMode succeeded!");
        }
        else
        {
            Debug.LogError("setMode failed!");
        }
    }

    // Close the device when the script is destroyed
    private void OnDestroy()
    {
#if UNITY_EDITOR
        return;
#endif
        closeDevice();
    }
}
