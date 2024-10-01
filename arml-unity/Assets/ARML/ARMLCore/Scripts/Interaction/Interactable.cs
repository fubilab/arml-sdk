using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using NaughtyAttributes;
using UltEvents;
using FishNet.Object;

namespace ARML
{
    /// <summary>
    /// An abstract base class for creating interactable objects in a game.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        protected Transform camera;
        public InteractionTimer iTimer;

        [Header("Interaction")]
        [SerializeField] public InteractionType interactionType;

        [ShowIf("interactionType", InteractionType.DWELL)]
        [Header("Dwell Time")]
        [SerializeField] protected float requiredInteractionTime = 1.5f;

        #region "Voice Commands"
        [ShowIf("interactionType", InteractionType.VOICE)]
        [Header("Voice Commands"), Tooltip("List of Voice Command Keywords that the Interactable will react to. Make sure each Keyword is just one word")]
        [SerializeField]
        protected List<VoiceCommandKeyword> voiceCommandKeywords = new List<VoiceCommandKeyword>();
        public static Action<string> OnVoiceCommandAction;

        [Serializable]
        public struct VoiceCommandKeyword
        {
            public Language language;
            public string keyword;
            public VoiceCommandAction action;
            public UltEvent FreeActionEvent;
        }

        [Serializable]
        public enum VoiceCommandAction
        {
            GRAB_OR_PLACE,
            USE,
            FREE
        }

        #endregion

        [Header("Model"), SerializeField]
        protected GameObject model;

        [Header("Visual")]
        [SerializeField] protected string interactingText = "Interacting...";
        [SerializeField] protected Material outlineMaterial;
        //[SerializeField] [Tooltip("The material slot in which the outline will be based")] protected int outlineMaterialIndex;
        [SerializeField] protected float outlineThickness = 1.5f;

        protected Material[] originalMaterials;
        protected Renderer renderer;

        [Header("Feedback")]
        [SerializeField] protected ActionFeedback actionFeedback;

        protected Transform cameraTransform;
        protected InteractionTimer interactionTimer;
        protected VoskSpeechToText voskSTT;

        protected CameraObjectSelectionController cameraController;

        [HideInInspector]
        public NetworkObject networkObject;

        /// <summary>
        /// Sets up voice command subscriptions if necessary.
        /// </summary>
        protected virtual void Awake()
        {
            InitializeComponents();
            InitializeInteractionTimer();
        }

        protected virtual void Start()
        {
            if (interactionType == InteractionType.VOICE)
            {
                AddVoiceCommandsToSTTKeywords();
            }
        }

        private void AddVoiceCommandsToSTTKeywords()
        {
            voskSTT = FindObjectOfType<VoskSpeechToText>(true);

            //Add keywords to voskSTT
            foreach (var command in voiceCommandKeywords)
            {
                if (command.keyword == "" || command.keyword.Contains(" "))
                    return;

                //Check if KeyPhrases already contains command keyword, if not, add it to list by language
                switch (command.language)
                {
                    case Language.EN:
                        if (!voskSTT.KeyPhrasesEN.Contains(command.keyword))
                            voskSTT.KeyPhrasesEN.Add(command.keyword);
                        break;
                    case Language.ES:
                        if (!voskSTT.KeyPhrasesES.Contains(command.keyword))
                            voskSTT.KeyPhrasesES.Add(command.keyword);
                        break;
                    case Language.CA:
                        if (!voskSTT.KeyPhrasesES.Contains(command.keyword))
                            voskSTT.KeyPhrasesES.Add(command.keyword);
                        break;
                }
            }

            STTMicController.OnVoiceCommandAction += CheckVoiceCommand;
        }

        private void InitializeComponents()
        {
            cameraTransform = Camera.main?.transform;
            interactionTimer = GetComponentInChildren<InteractionTimer>(true);
            renderer = model ? model.GetComponent<Renderer>() : GetComponent<Renderer>();
            cameraController = cameraTransform?.GetComponent<CameraObjectSelectionController>();
            originalMaterials = renderer ? renderer.materials : null;
            networkObject = GetComponent<NetworkObject>();
        }

        private void InitializeInteractionTimer()
        {
            if (interactionTimer)
            {
                interactionTimer.stateText.text = interactingText;
                interactionTimer.RequiredInteractionTime = requiredInteractionTime;
            }
        }

        /// <summary>
        /// Checks if the provided voice command matches the assigned keyword.
        /// </summary>
        /// <param name="command">The voice command to check.</param>
        protected virtual void CheckVoiceCommand(string command)
        {

        }

        /// <summary>
        /// Subscribes to necessary events when the object is enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            NetworkPlayer.OnPlayerLoaded += OnPlayerLoaded;
        }

        /// <summary>
        /// Unsubscribes from events when the object is disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            NetworkPlayer.OnPlayerLoaded -= OnPlayerLoaded;
            if (InteractionTypeController.Instance)
                InteractionTypeController.Instance.OnInteractionTypeChanged -= SwitchInteractionType;
        }

        /// <summary>
        /// Subscribes to interaction type changes after the player is loaded. 
        /// Needs to be done if using Mirror because gameobjects with NetworkIdentity are deactivated at the beginning so cant be subscribed to.
        /// </summary>
        protected void OnPlayerLoaded()
        {
            if (InteractionTypeController.Instance)
                InteractionTypeController.Instance.OnInteractionTypeChanged += SwitchInteractionType;
        }

        /// <summary>
        /// Switches the interaction type of the interactable object.
        /// </summary>
        protected void SwitchInteractionType(InteractionType newInteractionType)
        {
            interactionType = newInteractionType;
        }

        /// <summary>
        /// Adds an outline material to the renderer for visual feedback.
        /// </summary>
        protected void AddOutlineMaterial(Renderer renderer)
        {
            if (!renderer || !this.enabled) return;

            //If material number has already been edited, return
            if (renderer.materials.Length != originalMaterials.Length)
                return;

            Material[] newMaterials = new Material[originalMaterials.Length + 1];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                newMaterials[i] = originalMaterials[i];
            }
            newMaterials[originalMaterials.Length] = outlineMaterial;
            renderer.materials = newMaterials;
            //Change outline thickness
            renderer.materials[originalMaterials.Length].SetFloat("_Thickness", outlineThickness);
            //Set Render Queue due to weird issue with stencil
            renderer.materials[originalMaterials.Length].renderQueue = -1;

            if (interactionType != InteractionType.DWELL)
            {
                //Set Arduino colour
                Color arduinoColor = Color.magenta;
                ArduinoController.Instance?.SetArduinoColor(arduinoColor, 20f);
            }
        }

        /// <summary>
        /// Overload to remove outline material from a specified renderer.
        /// </summary>
        public void RemoveOutlineMaterial(Renderer renderer)
        {
            if (renderer == null)
                return;

            //If material number already original, return
            if (renderer.materials.Length == originalMaterials.Length)
                return;

            renderer.materials = originalMaterials;

            //Return if there is still one object selected
            if (cameraController == null)
            {
                cameraController = Camera.main.GetComponent<CameraObjectSelectionController>();
            }

            if (cameraController.currentlySelectedObjects.Count > 0)
                return;

            //Set Arduino colour
            if (ArduinoController.Instance.enabled)
                ArduinoController.Instance.SetArduinoColor(Color.clear, 0f);
        }

        /// <summary>
        /// Updates the outline fill based on the interaction timer.
        /// </summary>
        protected void UpdateOutlineFill(Renderer renderer)
        {
            if (!renderer) return;

            //If material number has already been edited, update fill and return
            if (renderer.materials.Length != originalMaterials.Length)
            {
                renderer.materials[originalMaterials.Length].SetFloat("_FillRate", iTimer.CurrentInteractionTime / iTimer.RequiredInteractionTime);
            }
        }

        /// <summary>
        /// Starts the interaction timer.
        /// </summary>
        protected void StartTimer()
        {
            if (!iTimer.IsInteracting)
                iTimer.StartInteraction();

            if (interactionType == InteractionType.DWELL)
                ArduinoController.Instance?.SetArduinoAnimation(Color.magenta, Color.yellow, 20, requiredInteractionTime, 1);
        }

        /// <summary>
        /// Stops the interaction timer.
        /// </summary>
        public void StopTimer()
        {
            if (iTimer.IsInteracting)
                iTimer.CancelInteraction();

            if (interactionType == InteractionType.DWELL)
                ArduinoController.Instance?.SetArduinoColor(Color.clear, 0f);
        }

        /// <summary>
        /// Deactivates the animator component, typically called as Event at the end of an animation clip to resolve issues with Rigidbody and animation interaction on grabbables.
        /// </summary>
        public void DeactivateAnimator()
        {
            Animator animator = GetComponent<Animator>();

            if (animator)
                animator.enabled = false;
        }

        public void DeactivateInteractable()
        {
            RemoveOutlineMaterial(renderer);
            enabled = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sets up the interaction timer and model children when this script is added to a GameObject in the Unity Editor.
        /// </summary>
        protected virtual void Reset()
        {
            if (GetComponentInChildren<InteractionTimer>(true) == null)
            {
                GameObject iTimerPrefab = PrefabUtility.InstantiatePrefab(Resources.Load("Interaction Timer"), transform) as GameObject;

                iTimer = iTimerPrefab.GetComponent<InteractionTimer>();
            }

            if (transform.Find("Model") == null)
            {
                //model = new GameObject("Model", typeof(MeshFilter), typeof(MeshRenderer));
                model = GameObject.CreatePrimitive(PrimitiveType.Cube);
                model.name = "Model";
                //model.GetComponent<MeshFilter>().mesh = PrimitiveType.Cube as Mesh;
                model.transform.SetParent(transform);
                model.transform.position = transform.position;
                model.SetActive(false);
            }

            if (string.IsNullOrEmpty(interactingText))
            {
                interactingText = "Interacting...";
            }
        }

        /// <summary>
        /// Sets up necessary components and default values in the Unity Editor when the Inspector is refreshed.
        /// </summary>
        protected void OnValidate()
        {
            if (iTimer == null)
            {
                iTimer = GetComponentInChildren<InteractionTimer>(true);
            }

            if (model == null)
            {
                transform.Find("Model");
            }

            if (outlineMaterial == null)
            {
                outlineMaterial = Resources.Load("M_Outline") as Material;
            }

            //Prevent inputting space on VoiceCommand keywords
            foreach (var command in voiceCommandKeywords)
            {
                if (command.keyword.Contains(" "))
                {
                    Debug.LogError($"A space was inputted in one of the VoiceCommandKeywords for {gameObject.name}. Make sure there are no spaces.");
                }
            }
        }
#endif
    }
}