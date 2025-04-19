using TMPro;
using UnityEngine;

namespace ARML.Interaction
{
    /// <summary>
    /// Controls the movement and behavior of the camera parent object, including cursor visibility and frame rate setup.
    /// </summary>
    public class CameraParentController : MonoBehaviour
    {
        public bool allowMove;
        public bool moveUpDown;

        [SerializeField] private float speed = 1f;
        [SerializeField] private bool isMouse = false;
        [SerializeField] private bool hideCursor = false;
        [SerializeField] private TMP_Text vectorText;

        /// <summary>
        /// Initializes the camera parent controller settings.
        /// </summary>
        private void Start()
        {
            SetupCursorVisibility();
            MoveToSceneOrigin();
            Invoke("SetupFrameRate", 0.5f);
        }

        /// <summary>
        /// Sets up cursor visibility based on platform and settings.
        /// </summary>
        private void SetupCursorVisibility()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return;
#else
        Cursor.visible = !hideCursor;
#endif
        }

        /// <summary>
        /// Configures the application's frame rate on specific platforms.
        /// </summary>
        private void SetupFrameRate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 61;
#endif
        }

        /// <summary>
        /// Called once per frame to handle camera movement, application exit, and updating the vector text.
        /// </summary>
        private void Update()
        {
//#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            if (allowMove)
                KeyboardControl();
//#endif

            if (vectorText)
            {
                vectorText.text = transform.position.ToString();
            }
        }

        /// <summary>
        /// Handles the movement and rotation of the camera based on keyboard input.
        /// </summary>
        private void KeyboardControl()
        {
            float moveSpeed = speed / 20;
            float rotateSpeed = speed / 2;
            
            Vector3 direction = Vector3.zero;

            // Move camera based on arrow key inputs
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                direction += Vector3.right;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                Vector3 v = moveUpDown ? Vector3.up : Vector3.forward;
                direction += v;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                Vector3 v = moveUpDown ? Vector3.down : Vector3.back;
                direction += v;
            }
            // Apply movement
            transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
            
            float rotationX = 0f;
            float rotationY = 0f;

            // Rotate camera based on arrow key inputs
            if (Input.GetKey(KeyCode.W))
            {
                rotationX = -1f;  // Rotate up
            }
            if (Input.GetKey(KeyCode.S))
            {
                rotationX = 1f;   // Rotate down
            }
            if (Input.GetKey(KeyCode.A))
            {
                rotationY = -1f;  // Rotate left
            }
            if (Input.GetKey(KeyCode.D))
            {
                rotationY = 1f;   // Rotate right
            }

            // Apply rotation
            transform.Rotate(rotationX * rotateSpeed * Time.deltaTime, rotationY * rotateSpeed * Time.deltaTime, 0);
            Vector3 currentRotation = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
        }

        /// <summary>
        /// Moves the camera to the scene origin if it exists.
        /// </summary>
        public void MoveToSceneOrigin()
        {
            Transform sceneOrigin = GameObject.Find("--ORIGIN--")?.transform;
            if (sceneOrigin)
            {
                transform.SetPositionAndRotation(sceneOrigin.position, sceneOrigin.rotation);
            }
        }

        public void SetMoveMode(bool upDown)
        {
            moveUpDown = upDown;
        }
    }
}