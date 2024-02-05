using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class FreePlacementObject : MonoBehaviour
{
    private DebugCanvasController debugCanvasController;
    private bool currentlyTriggered = false;
    private bool currentlyHeld = false;
    private float currentFloorHeight = -1;
    private Plane floorPlane;
    private const float maxDistance = 6f; // Maximum allowed distance

    private const float gridSize = 10; // Size of the grid to draw
    private const float squareSize = 0.5f; // Size of each square in the grid

    private GameObject debugPlane; // Add a public reference to the debug plane GameObject

    // Start is called before the first frame update
    void Start()
    {
        debugCanvasController = FindObjectOfType<DebugCanvasController>();
        debugPlane = debugCanvasController.debugPlane;
        if (debugCanvasController == null)
            Debug.LogError("DebugCanvasController not found. Make sure there is one in the scene");

        // Initialize the plane at the starting height
        floorPlane = new Plane(Vector3.up, new Vector3(0, Camera.main.transform.position.y + currentFloorHeight, 0));

        UpdateDebugPlaneHeight();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (currentlyTriggered && !currentlyHeld)
            {
                currentlyHeld = true;
                print("FreePlacing " + gameObject.name);
            }
            else if (currentlyHeld)
            {
                currentlyHeld = false;
            }
        }

        if (currentlyHeld)
        {
            float xAxisValue = Input.GetAxis("HorizontalArrow");
            float zAxisValue = Input.GetAxis("VerticalArrow");

            // Change floor height with VerticalArrow
            currentFloorHeight += zAxisValue * Time.deltaTime;

            // Update the plane's height
            floorPlane.SetNormalAndPosition(Vector3.up, new Vector3(0, currentFloorHeight, 0)); 

            // Calculate the ray from the camera's forward direction
            Ray cameraRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

            // Check if the ray intersects with the plane
            if (floorPlane.Raycast(cameraRay, out float enter))
            {
                // Get the intersection point
                Vector3 hitPoint = cameraRay.GetPoint(enter);

                // Calculate the point 5 meters away along the ray
                Vector3 pointAtMaxDistance = Camera.main.transform.position + Camera.main.transform.forward * maxDistance;

                // Check if the intersection point is further than the maximum allowed distance
                if (Vector3.Distance(Camera.main.transform.position, hitPoint) > maxDistance)
                {
                    hitPoint = pointAtMaxDistance; // Snap to the point 5 meters away
                }

                // Update the object's position and rotation
                transform.position = new Vector3(hitPoint.x, currentFloorHeight, hitPoint.z);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,
                        transform.localEulerAngles.y + (xAxisValue * Time.deltaTime),
                        transform.localEulerAngles.z);
            }

            debugPlane.SetActive(true);
            UpdateDebugPlaneHeight();
        }
        else
        {
            debugPlane.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CameraGrabber>() == null)
            return;

        currentlyTriggered = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CameraGrabber>() == null)
            return;

        currentlyTriggered = false;
    }
    private void UpdateDebugPlaneHeight()
    {
        if (debugPlane != null)
        {
            Vector3 planePosition = debugPlane.transform.position;
            planePosition.y = currentFloorHeight;
            debugPlane.transform.position = planePosition;
        }
    }

    void OnDrawGizmos()
    {
        //DrawCheckerPattern();
    }

    private void DrawCheckerPattern()
    {
        Vector3 position = new Vector3(0, currentFloorHeight, 0);
        bool toggleColor = false;

        for (float x = -gridSize / 2; x < gridSize / 2; x += squareSize)
        {
            for (float z = -gridSize / 2; z < gridSize / 2; z += squareSize)
            {
                Gizmos.color = toggleColor ? Color.black : Color.white;
                Vector3 squareCenter = new Vector3(x + squareSize / 2, currentFloorHeight, z + squareSize / 2);
                Gizmos.DrawCube(squareCenter, new Vector3(squareSize, 0.01f, squareSize));
                toggleColor = !toggleColor;
            }
            if (gridSize / squareSize % 2 == 0)
            {
                toggleColor = !toggleColor;
            }
        }
    }
}
