using AClockworkBerry;
using FishNet.Object;
using System;
using UnityEngine;
using ARML.Interaction;
using ARML.SceneManagement;

namespace ARML.Network
{
    /// <summary>
    /// Controls the admin user interface for various settings and interactions.
    /// </summary>
    public class AdminUIController : NetworkBehaviour
    {
        Camera cam;
        PostProcessingController postProcessingController;
        [SerializeField] GameObject virtualDouble;
        bool isButtonInteraction = false;

        #region Singleton
        public static AdminUIController Instance { get; private set; }

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
        #endregion

        private void Start()
        {
            cam = Camera.main;
            postProcessingController = FindObjectOfType<PostProcessingController>(true);
        }

        /// <summary>
        /// Sets the visibility of the canvas containing the admin UI.
        /// </summary>
        /// <param name="b">True to show the canvas, false to hide it.</param>
        public void SetCanvasVisibility(bool b)
        {
            gameObject.GetComponentInChildren<Canvas>().enabled = b;
        }

        //Required because of the button/toggle difference
        public void ToggleScreenLogger()
        {
            ScreenLogger.Instance.ShowLog = !ScreenLogger.Instance.ShowLog;
            CmdOnScreenLoggerToggled(ScreenLogger.Instance.ShowLog);
        }

        [ServerRpc(RequireOwnership = false)]
        void CmdOnScreenLoggerToggled(bool state)
        {
            ScreenLogger.Instance.ShowLog = state;
        }

        /// <summary>
        /// Toggles the use of physical camera properties.
        /// </summary>
        /// <param name="b">True to use physical properties, false otherwise.</param>
        public void TogglePhysicalCamera(bool b)
        {
            cam.usePhysicalProperties = b;
            CmdOnPhysicalCameraToggled(b);
        }

        [ServerRpc(RequireOwnership = false)]
        void CmdOnPhysicalCameraToggled(bool b)
        {
            cam.usePhysicalProperties = b;
            Debug.Log("Physical Camera: " + cam.usePhysicalProperties);
        }

        /// <summary>
        /// Changes the vertical lens shift of the camera.
        /// </summary>
        /// <param name="value">The new vertical lens shift value.</param>
        public void ChangeVerticalLensShift(float value)
        {
            //Round to 3 decimals and compare, if it's the same stop
            if ((float)Math.Round(value * 1000f) / 1000f == (float)Math.Round(cam.lensShift.y * 1000f) / 1000f)
                return;

            cam.lensShift = new Vector2(cam.lensShift.x, value);
            CmdOnChangeVerticalLensShift(cam.lensShift);
            Debug.Log(string.Format("Vertical Lens Shift: {0}", cam.lensShift.ToString("F3")));
        }

        [ServerRpc(RequireOwnership = false)]
        void CmdOnChangeVerticalLensShift(Vector2 shift)
        {
            cam.lensShift = shift;
            Debug.Log(string.Format("Vertical Lens Shift: {0}", cam.lensShift.ToString("F3")));
        }

        /// <summary>
        /// Toggles the vignette effect.
        /// </summary>
        /// <param name="b">True to enable the vignette effect, false to disable it.</param>
        public void ToggleVignette(bool b)
        {
            postProcessingController?.OnToggleVignette(b);
        }

        /// <summary>
        /// Changes the value of the contrast effect.
        /// </summary>
        /// <param name="value">The new value of the contrast effect.</param>
        public void ChangeValueContrast(float value)
        {
            postProcessingController.OnChangeValueContrast(value);
        }

        /// <summary>
        /// Toggles the visibility of the virtual double object.
        /// </summary>
        public void ToggleVirtualDouble()
        {
            virtualDouble.SetActive(!virtualDouble.activeSelf);
            CmdOnToggleVirtualDouble();
        }

        [ServerRpc(RequireOwnership = false)]
        void CmdOnToggleVirtualDouble()
        {
            virtualDouble.SetActive(!virtualDouble.activeSelf);
        }

        /// <summary>
        /// Toggles the interaction type between button and dwell.
        /// </summary>
        public void ToggleInteractionType()
        {
            isButtonInteraction = !isButtonInteraction;
            InteractionTypeController.Instance.ChangeInteractionType(isButtonInteraction);
            CmdOnToggleInteractionType(isButtonInteraction);
        }

        [ServerRpc(RequireOwnership = false)]
        void CmdOnToggleInteractionType(bool state)
        {
            InteractionTypeController.Instance.ChangeInteractionType(state);
        }

        /// <summary>
        /// Restarts the current scene.
        /// </summary>
        public void RestartCurrentScene()
        {
            SceneController.Instance.ResetCurrentSceneSingle();
            CmdOnRestartCurrentScene();
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdOnRestartCurrentScene()
        {
            SceneController.Instance.ResetCurrentSceneSingle();
            //ARMLNetworkManager.Instance.ServerChangeScene(SceneManager.GetActiveScene().name);
        }
    }
}