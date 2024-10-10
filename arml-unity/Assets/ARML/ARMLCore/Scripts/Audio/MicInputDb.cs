using TMPro;
using UnityEngine;

namespace ARML.Voice
{
    /// <summary>
    /// The MicInput class handles microphone input in Unity, providing functionality to capture the microphone's loudness 
    /// and convert it into both linear and decibel values. It also updates a UI text element with the microphone's loudness 
    /// in real-time.
    /// </summary>
    public class MicInput : MonoBehaviour
    {
        #region Singleton

        public static MicInput Instance { set; get; }

        #endregion

        public float MicLoudness;
        public float MicLoudnessinDecibels;

        private string _device;

        private TMP_Text dbText;

        private AudioClip _clipRecord = null;
        private AudioClip _recordedClip = null;
        private int _sampleWindow = 128;

        private bool _isInitialized;

        private void Start()
        {
            dbText = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// Initializes the microphone.
        /// </summary>
        public void InitMic()
        {
            if (_device == null && Microphone.devices.Length > 0)
            {
                _device = Microphone.devices[0];
            }

            if (_device == null)
                return;

            _clipRecord = Microphone.Start(_device, true, 999, 44100);
            _isInitialized = true;
        }

        /// <summary>
        /// Stops the microphone.
        /// </summary>
        public void StopMicrophone()
        {
            Microphone.End(_device);
            _isInitialized = false;
        }

        /// <summary>
        /// Gets the maximum microphone level.
        /// </summary>
        /// <returns>Maximum microphone level.</returns>
        private float MicrophoneLevelMax()
        {
            float levelMax = 0;
            float[] waveData = new float[_sampleWindow];
            int micPosition = Microphone.GetPosition(null) - (_sampleWindow + 1); // null means the first microphone
            if (micPosition < 0) return 0;
            _clipRecord.GetData(waveData, micPosition);

            // Getting a peak on the last 128 samples
            for (int i = 0; i < _sampleWindow; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }
            return levelMax;
        }

        /// <summary>
        /// Converts the microphone level to decibels.
        /// </summary>
        /// <returns>Microphone level in decibels.</returns>
        private float MicrophoneLevelMaxDecibels()
        {
            float db = 20 * Mathf.Log10(Mathf.Abs(MicLoudness));
            return db;
        }

        /// <summary>
        /// Calculates the linear level of an AudioClip.
        /// </summary>
        /// <param name="clip">The AudioClip to analyze.</param>
        /// <returns>Linear level of the AudioClip.</returns>
        public float FloatLinearOfClip(AudioClip clip)
        {
            StopMicrophone();

            _recordedClip = clip;

            float levelMax = 0;
            float[] waveData = new float[_recordedClip.samples];

            _recordedClip.GetData(waveData, 0);

            for (int i = 0; i < _recordedClip.samples; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }
            return levelMax;
        }

        /// <summary>
        /// Calculates the decibel level of an AudioClip.
        /// </summary>
        /// <param name="clip">The AudioClip to analyze.</param>
        /// <returns>Decibel level of the AudioClip.</returns>
        public float DecibelsOfClip(AudioClip clip)
        {
            StopMicrophone();

            _recordedClip = clip;

            float levelMax = 0;
            float[] waveData = new float[_recordedClip.samples];

            _recordedClip.GetData(waveData, 0);

            for (int i = 0; i < _recordedClip.samples; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }

            float db = 20 * Mathf.Log10(Mathf.Abs(levelMax));
            return db;
        }

        private void Update()
        {
            MicLoudness = MicrophoneLevelMax();
            MicLoudnessinDecibels = MicrophoneLevelMaxDecibels();

            dbText.text = MicLoudness.ToString("F2");
        }

        private void OnEnable()
        {
            InitMic();
            _isInitialized = true;
            Instance = this;
        }

        private void OnDisable()
        {
            StopMicrophone();
        }

        private void OnDestroy()
        {
            StopMicrophone();
        }

        /// <summary>
        /// Manages microphone state when the application gains or loses focus.
        /// </summary>
        /// <param name="focus">True if the application is focused, false otherwise.</param>
        private void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                if (!_isInitialized)
                {
                    InitMic();
                }
            }
            else
            {
                StopMicrophone();
            }
        }
    }
}
