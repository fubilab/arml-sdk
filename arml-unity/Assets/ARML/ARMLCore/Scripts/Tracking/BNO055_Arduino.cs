using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

namespace ARML.Tracking
{
    /// <summary>
    /// Handles communication with a BNO055 sensor via Arduino, updating the rotation of the GameObject.
    /// </summary>
    public class BNO055_Arduino : MonoBehaviour
    {
        /// <summary>
        /// The serial port used to communicate with the Arduino.
        /// </summary>
        private SerialPort serialPort;

        /// <summary>
        /// The thread used for reading data from the serial port.
        /// </summary>
        private Thread readThread;

        /// <summary>
        /// Flag to indicate if the reading thread is running.
        /// </summary>
        private bool isRunning = false;

        /// <summary>
        /// Quaternion values received from the sensor.
        /// </summary>
        private double qx, qy, qz, qw;

        /// <summary>
        /// The initial Euler angles of the camera.
        /// </summary>
        private Vector3 camStartEuler;

        /// <summary>
        /// The initial rotation value from the IMU.
        /// </summary>
        private Vector3 initialImuRotation = Vector3.zero;

        /// <summary>
        /// Singleton instance of BNO055_Arduino.
        /// </summary>
        public static BNO055_Arduino Instance { get; private set; }

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

        /// <summary>
        /// Initializes the serial port and starts the reading thread.
        /// </summary>
        void Start()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return;
#endif
            // Modify the port name as needed
            string portName = "/dev/ttyACM0";
            int baudRate = 115200;

            serialPort = new SerialPort(portName, baudRate);
            serialPort.WriteTimeout = 100;
            serialPort.ReadTimeout = 500;
            serialPort.DtrEnable = true;
            serialPort.RtsEnable = true;

            try
            {
                serialPort.Open();
                isRunning = true;
                Debug.Log("isRunning is true");
            }
            catch (Exception e)
            {
                Debug.LogError("Error opening serial port: " + e.Message);
            }

            readThread = new Thread(ReadSerialData);
            readThread.Start();
            Debug.Log("Thread Started");

            camStartEuler = transform.localRotation.eulerAngles;
        }

        /// <summary>
        /// Updates the rotation of the GameObject based on received quaternion data.
        /// </summary>
        void Update()
        {
            // Update the rotation of the GameObject based on received quaternion data
            if (!isRunning)
                return;

            Quaternion remappedImuRotation = new Quaternion(-(float)qy, -(float)qz, (float)qx, (float)qw);
            if (initialImuRotation == Vector3.zero)
            {
                initialImuRotation = new Quaternion(remappedImuRotation.x, remappedImuRotation.y, 0, remappedImuRotation.w).eulerAngles;
            }

            Vector3 correctedImuRotation = remappedImuRotation.eulerAngles - initialImuRotation;

            transform.localEulerAngles = correctedImuRotation + camStartEuler;

            // Reset Rotation
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Restarted rotation");
                initialImuRotation = remappedImuRotation.eulerAngles;
            }
        }

        /// <summary>
        /// Continuously reads data from the serial port in a separate thread.
        /// </summary>
        void ReadSerialData()
        {
            while (isRunning)
            {
                try
                {
                    string data = serialPort.ReadLine();
                    Debug.Log("data: " + data);
                    string[] values = data.Split(',');

                    if (values.Length == 4 && double.TryParse(values[0], out qx) && double.TryParse(values[1], out qy)
                        && double.TryParse(values[2], out qz) && double.TryParse(values[3], out qw))
                    {
                        // Debug.Log($"Quaternion values: qx={qx:F4}, qy={qy:F4}, qz={qz:F4}, qw={qw:F4}");
                    }
                    else
                    {
                        Debug.LogError("Failed to parse data: " + data);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error reading serial data: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Ensures proper cleanup of resources when the application quits.
        /// </summary>
        void OnApplicationQuit()
        {
            isRunning = false;

            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }

            if (readThread != null && readThread.IsAlive)
            {
                readThread.Join();
            }
        }
    }
}