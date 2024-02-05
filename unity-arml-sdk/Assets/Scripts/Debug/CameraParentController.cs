using TMPro;
using UnityEngine;

/// <summary>
/// Controls the movement and behavior of the camera parent object, including cursor visibility and frame rate setup.
/// </summary>
public class CameraParentController : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private bool isMouse = false;
    [SerializeField] private bool hideCursor = false;
    [SerializeField] private TMP_Text vectorText;

    public static CameraParentController Instance { get; private set; }

    /// <summary>
    /// Ensures that only one instance of the CameraParentController exists and persists across scenes.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy the GameObject if an instance already exists
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Initializes the camera parent controller settings.
    /// </summary>
    private void Start()
    {
        DontDestroyOnLoad(gameObject.transform.parent);
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
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        MoveCamera();
#endif
        HandleAppExit();

        if (vectorText)
        {
            vectorText.text = transform.position.ToString();
        }
    }

    /// <summary>
    /// Handles the movement and rotation of the camera based on input.
    /// </summary>
    private void MoveCamera()
    {
        float xAxisValue = isMouse ? Input.GetAxis("Mouse X") : Input.GetAxis("HorizontalArrow");
        float zAxisValue = isMouse ? Input.GetAxis("Mouse Y") : Input.GetAxis("VerticalArrow");

        transform.Rotate(-zAxisValue * speed * Time.deltaTime, xAxisValue * speed * Time.deltaTime, 0);
        Vector3 currentRotation = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);

        Vector3 movementVector = (transform.forward * Input.GetAxis("Vertical") * 10) +
                                 (transform.right * Input.GetAxis("Horizontal") * 10);
        transform.localPosition += new Vector3(movementVector.x, 0, movementVector.z) * Time.deltaTime;
    }

    /// <summary>
    /// Handles the application exit process.
    /// </summary>
    private void HandleAppExit()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
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
}
