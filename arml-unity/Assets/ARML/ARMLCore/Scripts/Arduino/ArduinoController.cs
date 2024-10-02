using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace ARML
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
        string portName = "COM7";

        [SerializeField, Tooltip("The baud rate for communication with the Arduino.")]
        int baudRate = 115200;

        [SerializeField, Tooltip("The timeout period (in milliseconds) for reading data from the Arduino.")]
        int readTimeOut = 500;

        [SerializeField, Tooltip("Time in seconds between messages sent to Arduino.")]
        float writeInterval = 0.2f;

        [SerializeField, Tooltip("If true, all received messages will be printed in the console.")]
        bool printAllMessages;

        [Header("Color Settings")]
        [SerializeField, Tooltip("The solid color used for the Arduino display.")]
        Color solidColor;

        [SerializeField, Tooltip("The progress color used during animations.")]
        Color progressColor;

        [Range(0, 254), SerializeField, Tooltip("Brightness level of the white channel.")]
        float whiteBrightness;

        [Range(0, 254), SerializeField, Tooltip("Overall brightness of the colors.")]
        float overallBrightness;

        [Header("Animation Settings")]
        [SerializeField, Tooltip("Enables or disables snake-style animation.")]
        bool isSnakeAnimation = false;

        [SerializeField, Tooltip("The direction of the animation, either forwards or backwards.")]
        AnimationDirection animationDirection;

        [SerializeField, Tooltip("The total number of pixels in the LED strip.")]
        int totalPixelsInStrip;

        [SerializeField, Tooltip("Time in seconds it takes for the entire animation to loop.")]
        float animationTime = 1f;

        [SerializeField, Tooltip("The length of the animation in pixels.")]
        int animationPixelLength = 1;

        [SerializeField, Tooltip("The starting pixel index for the animation.")]
        int animationStartPixelIndex = 0;

        [SerializeField, Tooltip("The ending pixel index for the animation.")]
        int animationEndPixelIndex = 72;

        [SerializeField, Tooltip("Turns off pixels outside the defined animation range.")]
        bool clearPixelsOutsideRange = false;

        [Space(10)]
        [Header("Scriptable Object Saving")]
        [SerializeField, Tooltip("A reference to the ArduinoAnimationSO ScriptableObject containing the animation settings.")]
        ArduinoAnimationSO animationSO;

        private List<string> messagesList = new List<string>();
        private int progressPixelIndex;
        private Coroutine sendMsgCoroutine;
        private SerialPort serialPort;
        private Thread readThread;
        private bool readThreadRunning;
        private string prevMsg;
        private bool readyToSend;

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

        // Start is called before the first frame update
        private void Start()
        {
            readyToSend = true;

            try
            {
                serialPort.Open();
                readThreadRunning = true;
                readThread = new Thread(ReadSerialData);
                readThread.Start();
                Debug.Log("Arduino Thread running");

                sendMsgCoroutine = StartCoroutine(SendMessageToArduino());
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error opening serial port: " + e.Message);
            }
        }

        /// <summary>
        /// Sends the microphone loudness level to the Arduino.
        /// </summary>
        private void SendMicLoudness()
        {
            SetArduinoBrightness(MicInput.Instance.MicLoudness);
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

                    // If last read is not last command, it was sent incorrectly, try again
                    if (readData.Contains("CMD"))
                    {
                        // Remove CMD: and line breaks
                        string splitData = readData.Substring(readData.IndexOf(":") + 2)
                            .Replace("\r\n", "").Replace("\r", "").Replace("\n", "");

                        if (splitData != prevMsg)
                        {
                            print($"ArduinoController: Sending again");
                            ForceSendMessageToArduino(prevMsg);
                        }
                        else
                        {
                            print($"ArduinoController: Can send next message");
                            readyToSend = true;
                        }
                    }
                }
                catch (Exception e)
                {
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

            if (msg == prevMsg)
            {
                sendMsgCoroutine = StartCoroutine(SendMessageToArduino());
                yield break;
            }

            if (serialPort.IsOpen)
            {
                serialPort.WriteLine(msg);
                print($"ArduinoController Sent message: {msg}");
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
                print($"ArduinoController Sent message: {msg}");
            }
        }

        private void OnDestroy()
        {
            whiteBrightness = 0f;
            overallBrightness = 0f;
            SetArduinoColor(Color.clear, 0f, true);
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

        private void OnValidate()
        {
            if (!Application.isPlaying) return;

            if (serialPort != null && serialPort.IsOpen)
                AddMessageToQueue(GetSolidColorCommand(solidColor));
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
                   $"A{overallBrightness:F0}" +
                   "E"; // E used to set end of command, used to prevent wrong colors by ignoring anything after this
        }

        /// <summary>
        /// Sets the Arduino to display a specified color.
        /// </summary>
        /// <param name="color">The color to set.</param>
        /// <param name="brightness">The brightness level.</param>
        /// <param name="force">If true, forces the command to be sent immediately, ignoring the queue.</param>
        public void SetArduinoColor(Color color, float brightness, bool force = false)
        {
            overallBrightness = brightness;
            if (!force)
                AddMessageToQueue(GetSolidColorCommand(color));
            else
                ForceSendMessageToArduino(GetSolidColorCommand(color));
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
        public void SetArduinoAnimation(Color bgColor, Color aColor, float brightness = -1, float rate = -1, int length = -1, int startPixelIndex = -1, int endPixelIndex = -1)
        {
            //Default value checks
            if (brightness == -1)
                brightness = overallBrightness;

            if (rate == -1)
                rate = animationTime;

            if (length == -1)
                length = animationPixelLength;

            if (startPixelIndex == -1)
                startPixelIndex = animationStartPixelIndex;

            if (endPixelIndex == -1)
                endPixelIndex = totalPixelsInStrip;

            if (endPixelIndex == 0)
                endPixelIndex = animationEndPixelIndex;

            overallBrightness = brightness;

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
        public void SetArduinoBrightness(float brightness)
        {
            overallBrightness = brightness;
            AddMessageToQueue(GetSolidColorCommand(solidColor));
        }

#if UNITY_EDITOR
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

            AssetDatabase.CreateAsset(asset, "Assets/Scripts/Arduino/Animations/NewArduinoAnimation.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
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