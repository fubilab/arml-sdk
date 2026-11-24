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
using HandTracker = ARML.HandTracking.HandTracker;
using TrackedHandPose = ARML.HandTracking.TrackedHandPose;

namespace ARML.Arduino
{
    /// <summary>
    /// What the LED strip is used for.
    /// </summary>
    public enum ArduinoLedMode
    {
        /// <summary>The lights show when an object is interactable (the scene scripts drive the strip).</summary>
        Interactable,

        /// <summary>
        /// The lights show the hand tracking quality: off until a hand is tracked, then yellow for the left hand,
        /// green for the right hand and half and half for both. The interactable lights are ignored.
        /// </summary>
        HandTrackingQuality
    }

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

        [Header("LED Mode")]
        [SerializeField, OnValueChanged("onLedModeChanged"),
         Tooltip("Interactable: the lights show when an object is interactable (default). " +
                 "HandTrackingQuality: the lights show the hand tracking quality and the interactable lights are ignored.")]
        private ArduinoLedMode ledMode = ArduinoLedMode.Interactable;

        [Header("Hand Tracking Quality Settings")]
        [SerializeField, ShowIf("isHandQualityMode"), Tooltip("Strip color while the left hand is tracked.")]
        private Color leftHandColor = new Color(1f, 0.7f, 0f);

        [SerializeField, ShowIf("isHandQualityMode"), Tooltip("Strip color while the right hand is tracked.")]
        private Color rightHandColor = Color.green;

        [SerializeField, ShowIf("isHandQualityMode"),
         Tooltip("Strip color while no hand is tracked. Black turns the lights off.")]
        private Color noHandColor = Color.black;

        [SerializeField, ShowIf("isHandQualityMode"),
         Tooltip("When both hands are tracked, the left hand color is on the first half of the strip and the right " +
                 "hand color on the second half. Enable to swap the halves.")]
        private bool swapHandHalves;

        [SerializeField, ShowIf("isHandQualityMode"), Range(0f, 1f),
         Tooltip("Hands with a lower landmark confidence do not count as detected.")]
        private float handMinConfidence = 0.6f;

        [SerializeField, ShowIf("isHandQualityMode"), Min(0f),
         Tooltip("A hand must be detected this long (seconds) before its light turns on. Avoids flashes.")]
        private float handConfirmTime = 0.1f;

        [SerializeField, ShowIf("isHandQualityMode"), Min(0f),
         Tooltip("A hand can be lost for this long (seconds) before its light turns off. Avoids flicker.")]
        private float handLostGraceTime = 0.25f;

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
            if (ledMode == ArduinoLedMode.HandTrackingQuality)
                UpdateHandQualityLights();

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

        #region LED Mode

        /// <summary>
        /// What the LED strip shows. Can be changed at runtime.
        /// </summary>
        public ArduinoLedMode LedMode
        {
            get => ledMode;
            set
            {
                if (ledMode == value) return;
                ledMode = value;
                if (Application.isPlaying) ApplyLedMode();
            }
        }

        private bool isHandQualityMode => ledMode == ArduinoLedMode.HandTrackingQuality;

        private void onLedModeChanged()
        {
            if (!Application.isPlaying) return;
            ApplyLedMode();
        }

        /// <summary>
        /// Drops the lights of the previous mode that are still waiting in the queue and starts the new mode.
        /// </summary>
        private void ApplyLedMode()
        {
            messagesList.RemoveAll(IsLightMessage);
            ResetHandQualityLights();

            // Hand quality lights are sent by Update, the interactable ones go back to the default lights
            if (ledMode == ArduinoLedMode.Interactable)
                SetArduinoDefault();
        }

        /// <summary>
        /// True for messages that change the lights, false for messages about the state of the Arduino.
        /// </summary>
        private static bool IsLightMessage(string msg)
        {
            return !(msg.StartsWith("ARML_READY") || msg.StartsWith("ARML_LOADING") ||
                     msg.StartsWith("ARML_B") || msg.StartsWith("ARML_ENABLE_BNO"));
        }

        /// <summary>
        /// Tracks if a hand counts as detected, with a delay before it turns on and a grace time before it turns off.
        /// </summary>
        private sealed class HandPresence
        {
            public bool Active { get; private set; }
            private float goodSince = -1f;
            private float lastGoodTime;

            public bool Update(bool good, float now, float confirmTime, float graceTime)
            {
                if (good)
                {
                    if (goodSince < 0f) goodSince = now;
                    lastGoodTime = now;
                    if (!Active && now - goodSince >= confirmTime) Active = true;
                }
                else
                {
                    goodSince = -1f;
                    if (Active && now - lastGoodTime > graceTime) Active = false;
                }
                return Active;
            }

            public void Reset()
            {
                Active = false;
                goodSince = -1f;
            }
        }

        private const string SplitCommandPrefix = "ARML_SPLIT_";
        private const int HandStateUnknown = -1;
        private const int HandStateLeft = 1;
        private const int HandStateRight = 2;

        private readonly HandPresence leftPresence = new HandPresence();
        private readonly HandPresence rightPresence = new HandPresence();
        private int sentHandState = HandStateUnknown;

        /// <summary>
        /// Forgets the hand state, so the lights are sent again with the next update.
        /// </summary>
        private void ResetHandQualityLights()
        {
            leftPresence.Reset();
            rightPresence.Reset();
            sentHandState = HandStateUnknown;
        }

        private bool IsHandDetected(TrackedHandPose pose)
        {
            return pose.Tracked && pose.Confidence >= handMinConfidence;
        }

        /// <summary>
        /// Sends the hand tracking quality to the Arduino when it changed.
        /// </summary>
        private void UpdateHandQualityLights()
        {
            HandTracker tracker = HandTracker.Instance;
            float now = Time.unscaledTime;

            bool left = leftPresence.Update(tracker != null && IsHandDetected(tracker.Left),
                now, handConfirmTime, handLostGraceTime);
            bool right = rightPresence.Update(tracker != null && IsHandDetected(tracker.Right),
                now, handConfirmTime, handLostGraceTime);

            int state = (left ? HandStateLeft : 0) | (right ? HandStateRight : 0);
            if (state == sentHandState) return;
            sentHandState = state;

            Color firstHalf, secondHalf;
            if (left && right)
            {
                firstHalf = swapHandHalves ? rightHandColor : leftHandColor;
                secondHalf = swapHandHalves ? leftHandColor : rightHandColor;
            }
            else if (left)
                firstHalf = secondHalf = leftHandColor;
            else if (right)
                firstHalf = secondHalf = rightHandColor;
            else
                firstHalf = secondHalf = noHandColor;

            // Only the newest hand state matters, drop older ones that were not sent yet
            messagesList.RemoveAll(msg => msg.StartsWith(SplitCommandPrefix));
            AddMessageToQueue($"{SplitCommandPrefix}{GetColorCommand(firstHalf, 0)}_{GetColorCommand(secondHalf, 0)}");
        }

        #endregion

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
            ResetHandQualityLights();

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
            return GetColorCommand(solidColor, whiteBrightness);
        }

        /// <summary>
        /// Generates the color part of a command: red, green, blue, white and alpha.
        /// </summary>
        /// <param name="color">The color to set.</param>
        /// <param name="white">The brightness of the white channel.</param>
        /// <returns>The command string.</returns>
        private static string GetColorCommand(Color color, int white)
        {
            return $"R{(color.r * 255):F0}" +
                   $"G{(color.g * 255):F0}" +
                   $"B{(color.b * 255):F0}" +
                   $"W{white:F0}" +
                   $"A{(color.a * 255):F0}" +
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
        /// Ignored in <see cref="ArduinoLedMode.HandTrackingQuality"/> mode, where the strip shows the hand tracking.
        /// </summary>
        /// <param name="color">The color to set.</param>
        /// <param name="brightness">The brightness level.</param>
        /// <param name="force">If true, forces the command to be sent immediately, ignoring the queue.</param>
        public void SetArduinoColor(Color color, bool force = false)
        {
            if (isHandQualityMode) return;

            solidColor = color;
            if (!force)
                AddMessageToQueue(GetSolidColorCommand(color));
            else
                ForceSendMessageToArduino(GetSolidColorCommand(color));
        }

        /// <summary>
        /// Sets the Arduino to display default lights.
        /// Ignored in <see cref="ArduinoLedMode.HandTrackingQuality"/> mode, where the strip shows the hand tracking.
        /// </summary>
        /// <param name="force">If true, forces the command to be sent immediately, ignoring the queue.</param>
        public void SetArduinoDefault(bool force = false)
        {
            if (isHandQualityMode) return;

            if (!force)
                AddMessageToQueue("ARML_DEFAULT");
            else
                ForceSendMessageToArduino("ARML_DEFAULT");
        }

        /// <summary>
        /// Animates a fade from a color to another over a given duration.
        /// Ignored in <see cref="ArduinoLedMode.HandTrackingQuality"/> mode, where the strip shows the hand tracking.
        /// </summary>
        /// <param name="from">Color to fade from.</param>
        /// <param name="to">Color to fade to.</param>
        /// <param name="duration">Duration of the fade in seconds.</param>
        public void SetArduinoFade(Color from, Color to, float duration)
        {
            if (isHandQualityMode) return;

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
        /// Ignored in <see cref="ArduinoLedMode.HandTrackingQuality"/> mode, where the strip shows the hand tracking.
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
            if (isHandQualityMode) return;

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