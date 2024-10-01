using System;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Manages the global language settings for dialogue and voice interactions.
    /// </summary>
    public class LanguageController : MonoBehaviour
    {
        /// <summary>
        /// The currently selected language for voice interactions.
        /// </summary>
        public Language currentLanguage;

        /// <summary>
        /// Reference to the Vosk speech-to-text component for handling voice recognition.
        /// </summary>
        public VoskSpeechToText voskSTT;

        /// <summary>
        /// Singleton instance of the LanguageController.
        /// </summary>
        public static LanguageController Instance { get; private set; }

        private void Awake()
        {
            InitializeSingleton();
            SetVoskModelPath();
        }

        /// <summary>
        /// Sets the model path and key phrases for the Vosk speech recognition based on the current language.
        /// </summary>
        private void SetVoskModelPath()
        {
            switch (currentLanguage)
            {
                case Language.EN:
                    voskSTT.ModelPath = voskSTT.ModelPathEN;
                    voskSTT.KeyPhrases = voskSTT.KeyPhrasesEN;
                    break;
                case Language.ES:
                    voskSTT.ModelPath = voskSTT.ModelPathES;
                    voskSTT.KeyPhrases = voskSTT.KeyPhrasesES;
                    break;
                case Language.CA:
                    voskSTT.ModelPath = voskSTT.ModelPathES; 
                    voskSTT.KeyPhrases = voskSTT.KeyPhrasesES; 
                    break;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the LanguageController.
        /// Ensures that only one instance exists and persists across scenes.
        /// </summary>
        private void InitializeSingleton()
        {
            if (!Application.isPlaying)
            {
                return; // Prevents singleton behavior in the editor
            }

            // If an instance already exists and it's not this one, destroy this object.
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Makes this instance persistent across scene loads
            }
        }

        /// <summary>
        /// Updates the current language and restarts the Vosk model if the language changes.
        /// </summary>
        /// <param name="language">The new language to set, represented as an integer.</param>
        public void SetLanguage(int language)
        {
            // Return if the selected language is already the current one.
            if (currentLanguage == (Language)language)
                return;

            // Update the current language and restart Vosk model.
            currentLanguage = (Language)language;
            Debug.Log($"Changed Language to {currentLanguage}");

            SetVoskModelPath(); // Set the Vosk model path for the new language.
            voskSTT.ChangeModel(); // Restart Vosk with the new model.

            // Reset the task list language in TaskProgressCanvas if it exists.
            if (TaskProgressCanvas.Instance)
                TaskProgressCanvas.Instance.ResetTaskListLanguage();
        }
    }

    /// <summary>
    /// Enum representing the supported languages.
    /// </summary>
    [Serializable]
    public enum Language
    {
        EN, // English
        ES, // Spanish
        CA  // Catalan
    }
}
