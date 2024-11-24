using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using ARML.Voice;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;

namespace ARML.Arduino
{
    /// <summary>
    /// The ArduinoController class is responsible for managing communication between Unity and an Arduino device. 
    /// It handles sending color and animation commands to the Arduino, receiving feedback, and managing LED displays.
    /// The class provides methods to control solid colors, animations, brightness levels, and other LED strip settings.
    /// It also supports message queuing and ensures reliable communication via serial port interaction.
    /// </summary>
    public class ArduinoController : MonoBehaviour
    {
        [Header("Arduino Settings")]
        [SerializeField, Tooltip("The port name to which the Arduino is connected, e.g., COM7.")]
        private string portName = "COM7";

        [SerializeField, Tooltip("The baud rate for communication with the Arduino.")]
        private int baudRate = 115200;

        [SerializeField, Tooltip("The timeout period (in milliseconds) for reading data from the Arduino.")]
        private int readTimeOut = 500;

        [SerializeField, Tooltip("Time in seconds between messages sent to Arduino.")]
        private float writeInterval = 0.2f;

        [SerializeField, Tooltip("If true, all received messages will be printed in the console.")]
        private bool printAllMessages;

        [Header("Color Settings")] [SerializeField, Tooltip("The solid color used for the Arduino display."),
            OnValueChanged("onSolidColorChanged")]
        private Color solidColor;
        private void onSolidColorChanged()
        {
            if (!Application.isPlaying) return;
            if (!previewSolidColor) return;
            SetArduinoColor(solidColor);
        }

        [SerializeField, Tooltip("The progress color used during animations.")]
        private Color progressColor;

        [Range(0, 254), SerializeField, Tooltip("Brightness level of the white channel."),
             OnValueChanged("onSolidColorChanged")]
        private int whiteBrightness;

        [SerializeField, Range(0, 254), Tooltip("Overall brightness of the colors."),
            OnValueChanged("onOverallBrightnessChanged")]
        private int overallBrightness;
        private void onOverallBrightnessChanged()
        {
            SetArduinoBrightness(overallBrightness);
        }
        

        [SerializeField] private float fadeLength = 2f;

        [SerializeField, Tooltip("Set solid color on button down.")]
        private bool solidColorOnButtonClick;
        
        [SerializeField, Tooltip("Preview changes to the color and white level (play mode only)."),
            OnValueChanged("onPreviewSolidColorChanged")]
        private bool previewSolidColor;
        private void onPreviewSolidColorChanged()
        {
            if (previewSolidColor)
                SetArduinoColor(solidColor);
        }

        [Header("Animation Settings")] [SerializeField, Tooltip("Enables or disables snake-style animation.")]
        private bool isSnakeAnimation = false;

        [SerializeField, Tooltip("The direction of the animation, either forwards or backwards.")]
        private AnimationDirection animationDirection;

        [SerializeField, Tooltip("The total number of pixels in the LED strip.")]
        private int totalPixelsInStrip;

        [SerializeField, Tooltip("Time in seconds it takes for the entire animation to loop.")]
        private float animationTime = 1f;

        [SerializeField, Tooltip("The length of the animation in pixels.")]
        private int animationPixelLength = 1;

        [SerializeField, Tooltip("The starting pixel index for the animation.")]
        private int animationStartPixelIndex = 0;

        [SerializeField, Tooltip("The ending pixel index for the animation.")]
        private int animationEndPixelIndex = 72;

        [SerializeField, Tooltip("Turns off pixels outside the defined animation range.")]
        private bool clearPixelsOutsideRange = false;

        [Space(10)]
        [Header("Scriptable Object Saving")]
        [SerializeField,
         Tooltip("A reference to the ArduinoAnimationSO ScriptableObject containing the animation settings.")]
        private ArduinoAnimationSO animationSO;

        [SerializeField,
         Tooltip("Filename of ScriptableObject for animation.")]
        private string animationName;

        [SerializeField,
         Tooltip("Path to save ScriptableObject files for animations.")]
        private string animationFilePath;

        private List<string> messagesList = new List<string>();
        private int progressPixelIndex;
        private Coroutine sendMsgCoroutine;
        private SerialPort serialPort;
        private Thread readThread;
        private bool readThreadRunning;
        private string prevMsg;
        private bool readyToSend;

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
        /// The current rotation value from the IMU.
        /// </summary>
        public Quaternion remappedImuRotation;

        /// <summary>
        /// The computed orientation euler angles from the IMU.
        /// </summary>
        public Vector3 bnoEulerAngles;

        #region Singleton

        public static ArduinoController Instance;

        /// <summary>
        /// Ensures only one instance of ArduinoController exists.
        /// </summary>
        private void Singleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy the GameObject if an instance already exists
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Optionally make it persistent
            }
        }

        #endregion

        private void Awake()
        {
            Singleton();

            serialPort = new SerialPort(portName, baudRate)
            {
                ReadTimeout = readTimeOut,
                WriteTimeout = 100,
                DtrEnable = true,
                RtsEnable = true
            };
        }

        private bool buttonDown = false;
        private void Update()
        {

            // trigger lights on click
            if (solidColorOnButtonClick)
            {
                if (!buttonDown && Input.GetMouseButtonDown(0))
                {
                    buttonDown = true;
                    SetArduinoColor(solidColor);
                }
                if (buttonDown && Input.GetMouseButtonUp(0))
                {
                    buttonDown = false;
                    SetArduinoDefault();
                }
            }

            // handle BNO sensor interpretation
            remappedImuRotation = new Quaternion(-(float)qy, -(float)qz, (float)qx, (float)qw);
            if (initialImuRotation == Vector3.zero)
            {
                initialImuRotation = new Quaternion(remappedImuRotation.x, remappedImuRotation.y, 0, remappedImuRotation.w).eulerAngles;
            }
            Vector3 correctedImuRotation = remappedImuRotation.eulerAngles - initialImuRotation;
            bnoEulerAngles = correctedImuRotation + camStartEuler;
        }

        public void ActivateBNO()
        {
            AddMessageToQueue("ARML_ENABLE_BNO");
        }

        public void ResetOrientation() 
        {
            initialImuRotation = remappedImuRotation.eulerAngles;
        }

        /// <summary>
        /// Sends the microphone loudness level to the Arduino.
        /// </summary>
        private void SendMicLoudness()
        {
            SetArduinoBrightness((int)MicInput.Instance.MicLoudness);
        }

        /// <summary>
        /// Continuously reads data from the serial port.
        /// </summary>
        private void ReadSerialData()
        {
            while (readThreadRunning)
            {
                try
                {
                    string readData = serialPort.ReadLine();
                    if (printAllMessages)
                        print(readData);

                    // read LED-related commands
                    if (readData.Contains("CMD"))
                    {
                        // Remove CMD: and line breaks
                        string splitData = readData.Substring(readData.IndexOf(":") + 2)
                            .Replace("\r\n", "").Replace("\r", "").Replace("\n", "");

                        // If last read is not last command, it was sent incorrectly, try again
                        if (splitData != prevMsg)
                        {
                            // print($"ArduinoController: Sending again");
                            ForceSendMessageToArduino(prevMsg);
                        }
                        else
                        {
                            // print($"ArduinoController: Can send next message");
                            readyToSend = true;
                        }
                    }
                    // try to parse quaternion values from BNO sensor
                    else
                    {
                        string[] values = readData.Split(',');
                        bool parsed = values.Length == 4
                            && double.TryParse(values[0], out qx) 
                            && double.TryParse(values[1], out qy)
                            && double.TryParse(values[2], out qz) 
                            && double.TryParse(values[3], out qw);
                    }
                }
                catch (Exception e)
                {
                    if (!e.Message.Contains("timed out"))
                        Debug.LogError("Error reading serial data: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Adds a message to the queue.
        /// </summary>
        /// <param name="msg">The message to be added to the queue.</param>
        private void AddMessageToQueue(string msg)
        {
            if (messagesList.Count > 0 && msg == messagesList.Last())
            {
                return;
            }
            // print("[ARDUINO] adding msg to q: " + msg);
            messagesList.Add(msg);
        }

        /// <summary>
        /// Coroutine to send messages to the Arduino at regular intervals.
        /// </summary>
        private IEnumerator SendMessageToArduino()
        {
            yield return new WaitForSeconds(writeInterval);

            if (!readyToSend || messagesList.Count == 0)
            {
                sendMsgCoroutine = StartCoroutine(SendMessageToArduino());
                yield break;
            }

            string msg = messagesList.First();
            messagesList.RemoveAt(0);

            // print("[ARDUINO] processing msg in q: " + msg);

            if (serialPort.IsOpen)
            {
                serialPort.WriteLine(msg);
                //($"ArduinoController Sent message: {msg}");
                prevMsg = msg;
                readyToSend = false;
            }

            sendMsgCoroutine = StartCoroutine(SendMessageToArduino());
        }

        /// <summary>
        /// Sends a message to the Arduino immediately, bypassing the queue.
        /// </summary>
        /// <param name="msg">The message to be sent.</param>
        private void ForceSendMessageToArduino(string msg)
        {
            if (serialPort.IsOpen)
            {
                serialPort.WriteLine(msg);
                // print($"ArduinoController Sent message: {msg}");
            }
        }

        private void OnEnable()
        {
            readyToSend = true;

            try
            {
                serialPort.Open();
                readThreadRunning = true;
                readThread = new Thread(ReadSerialData);
                readThread.Start();
                // Debug.Log("Arduino Thread running");

                sendMsgCoroutine = StartCoroutine(SendMessageToArduino());

                SetArduinoBrightness(overallBrightness);
                SetArduinoReady(true);
                SetArduinoDefault();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error opening serial port: " + e.Message);
            }
        }

        private void OnDisable()
        {
            try
            {
                // whiteBrightness = 0f;
                // overallBrightness = 0f;
                // SetArduinoColor(Color.clear, 0f, true);
                readThreadRunning = false;

                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                }

                if (readThread != null && readThread.IsAlive)
                {
                    readThread.Join();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error closing serial port: " + e.Message);
            }
        }

        /// <summary>
        /// Generates the command string to set a solid color on the Arduino.
        /// </summary>
        /// <param name="solidColor">The color to set.</param>
        /// <returns>The command string.</returns>
        private string GetSolidColorCommand(Color solidColor)
        {
            return $"R{(solidColor.r * 255):F0}" +
                   $"G{(solidColor.g * 255):F0}" +
                   $"B{(solidColor.b * 255):F0}" +
                   $"W{whiteBrightness:F0}" +
                   $"A{(solidColor.a * 255):F0}" +
                   "E"; // E used to set end of command, used to prevent wrong colors by ignoring anything after this
        }

        /// <summary>
        /// Sends message to Arduino to transition between loading animation and default lights.
        /// </summary>
        /// <param name="ready">Ready state to send to the Arduino</param>
        /// <param name="force">If true, forces the command to be sent immediately, ignoring the queue.</param>
        public void SetArduinoReady(bool ready, bool force = false)
        {
            string msg = ready ? "ARML_READY" : "ARML_LOADING";
            if (!force)
                AddMessageToQueue(msg);
            else
                ForceSendMessageToArduino(msg);
        }

        /// <summary>
        /// Sets the Arduino to display a specified color.
        /// </summary>
        /// <param name="color">The color to set.</param>
        /// <param name="brightness">The brightness level.</param>
        /// <param name="force">If true, forces the command to be sent immediately, ignoring the queue.</param>
        public void SetArduinoColor(Color color, bool force = false)
        {
            solidColor = color;
            if (!force)
                AddMessageToQueue(GetSolidColorCommand(color));
            else
                ForceSendMessageToArduino(GetSolidColorCommand(color));
        }

        /// <summary>
        /// Sets the Arduino to display default lights.
        /// </summary>
        /// <param name="force">If true, forces the command to be sent immediately, ignoring the queue.</param>
        public void SetArduinoDefault(bool force = false)
        {
            if (!force)
                AddMessageToQueue("ARML_DEFAULT");
            else
                ForceSendMessageToArduino("ARML_DEFAULT");
        }

        /// <summary>
        /// Animates a fade from a color to another over a given duration.
        /// </summary>
        /// <param name="from">Color to fade from.</param>
        /// <param name="to">Color to fade to.</param>
        /// <param name="duration">Duration of the fade in seconds.</param>
        public void SetArduinoFade(Color from, Color to, float duration)
        {
            SetArduinoColor(from);
            int fadeSteps = Mathf.RoundToInt(duration * 10f - 2f);
            for (int i = 0; i < fadeSteps; i++)
            {
                float lerpValue = (1f / fadeSteps * (i + 1f));
                Color lerpColor = Color.Lerp(from, to, lerpValue);
                SetArduinoColor(lerpColor);
            }
            SetArduinoColor(to);
        }

        /// <summary>
        /// Sets the Arduino animation with specified parameters.
        /// </summary>
        /// <param name="bgColor">The background color.</param>
        /// <param name="aColor">The animation color.</param>
        /// <param name="brightness">The brightness level.</param>
        /// <param name="rate">The animation rate.</param>
        /// <param name="length">The length of the animation.</param>
        /// <param name="startPixelIndex">The starting pixel index.</param>
        /// <param name="endPixelIndex">The ending pixel index.</param>
        public void SetArduinoAnimation(Color bgColor, Color aColor, float rate = -1,
            int length = -1, int startPixelIndex = -1, int endPixelIndex = -1)
        {
            if (rate == -1)
                rate = animationTime;

            if (length == -1)
                length = animationPixelLength;

            if (startPixelIndex == -1)
                startPixelIndex = animationStartPixelIndex;

            if (endPixelIndex == -1)
                endPixelIndex = totalPixelsInStrip;

            if (endPixelIndex == -1)
                endPixelIndex = animationEndPixelIndex;

            string animMode = isSnakeAnimation ? "Anim2" : "Anim1";

            if (animationDirection == AnimationDirection.BACKWARDS)
                animMode += "Ba";

            string cmd = $"{animMode}_S_{GetSolidColorCommand(bgColor)}" +
                         $"_X_{GetSolidColorCommand(aColor)}_" +
                         $"H{rate}_L{length}_PS{startPixelIndex}_PE{endPixelIndex}";

            if (clearPixelsOutsideRange)
                cmd += "_T";

            AddMessageToQueue(cmd);
        }

        /// <summary>
        /// Sets the brightness of the Arduino display.
        /// </summary>
        /// <param name="brightness">The brightness level.</param>
        /// <param name="force">If true, forces the command to be sent immediately, ignoring the queue.</param>
        public void SetArduinoBrightness(int brightness, bool force = false)
        {
            overallBrightness = brightness;
            // print("[ARDUINO] set brightness: " + overallBrightness);
            string cmd = $"ARML_B{overallBrightness}";
            if (!force) 
                AddMessageToQueue(cmd);
            else
                ForceSendMessageToArduino(cmd);
        }

#if UNITY_EDITOR
        [Button]
        private void TestSolidColor()
        {
            SetArduinoColor(solidColor);
        }

        [Button]
        private void TestFade()
        {
            SetArduinoFade(solidColor, progressColor, fadeLength);
        }

        [Button]
        private void TestAnimation()
        {
            SetArduinoAnimation(solidColor, progressColor, animationTime,
                animationPixelLength, animationStartPixelIndex, animationEndPixelIndex);
        }

        [Button]
        private void SaveAnimationSO()
        {
            ArduinoAnimationSO asset = ScriptableObject.CreateInstance<ArduinoAnimationSO>();

            asset.solidColor = solidColor;
            asset.progressColor = progressColor;
            asset.whiteBrightness = whiteBrightness;
            asset.overallBrightness = overallBrightness;
            asset.isSnakeAnimation = isSnakeAnimation;
            asset.animationDirection = animationDirection;
            asset.totalPixelsInStrip = totalPixelsInStrip;
            asset.animationTime = animationTime;
            asset.animationPixelLength = animationPixelLength;
            asset.animationStartPixelIndex = animationStartPixelIndex;
            asset.animationEndPixelIndex = animationEndPixelIndex;
            asset.clearPixelsOutsideRange = clearPixelsOutsideRange;

            animationSO = asset;

            AssetDatabase.CreateAsset(asset, animationFilePath + animationName + ".asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            //Selection.activeObject = asset;
        }

        [Button]
        private void LoadAnimationSO()
        {
            if (animationSO == null)
                return;

            solidColor = animationSO.solidColor;
            progressColor = animationSO.progressColor;
            whiteBrightness = animationSO.whiteBrightness;
            overallBrightness = animationSO.overallBrightness;
            isSnakeAnimation = animationSO.isSnakeAnimation;
            animationDirection = animationSO.animationDirection;
            totalPixelsInStrip = animationSO.totalPixelsInStrip;
            animationTime = animationSO.animationTime;
            animationPixelLength = animationSO.animationPixelLength;
            animationStartPixelIndex = animationSO.animationStartPixelIndex;
            animationEndPixelIndex = animationSO.animationEndPixelIndex;
            clearPixelsOutsideRange = animationSO.clearPixelsOutsideRange;
        }
#endif
    }
}