using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompassController : MonoBehaviour
{
    public Transform compassTarget;
    [SerializeField] RectTransform arrow;
    [SerializeField] float multiplier;

    private Camera cam;
    private bool targetIsBehind;

    #region Singleton
    public static CompassController Instance { get; private set; }

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
            DontDestroyOnLoad(transform.parent);
        }
    }
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
            return;
        }

        if (compassTarget != null && arrow != null)
        {
            // Calculate world direction vector from the camera to the target
            Vector3 worldDirection = compassTarget.position - cam.transform.position;

            // Transform the world direction into the camera's local space
            Vector3 localDirection = cam.transform.InverseTransformDirection(worldDirection);

            // Use localDirection.x and localDirection.z to calculate the angle in the camera's local space, ignoring Y axis
            float angle = Mathf.Atan2(localDirection.z, localDirection.x);

            // Convert angle to degrees. Subtracting 90 degrees to align with Unity's coordinate system if your forward vector is aligned with Y-axis.
            float angleDegrees = angle * Mathf.Rad2Deg - 90;

            angleDegrees *= multiplier;

            //Clamps for when target is behind camera
            if (angleDegrees < -80 && angleDegrees > -180 * multiplier)
            {
                angleDegrees = -80;
                targetIsBehind = true;
            }
            else if (angleDegrees > 80 || angleDegrees < -180 * multiplier)
            {
                angleDegrees = 80;
                targetIsBehind = true;
            }
            else
            {
                targetIsBehind = false;
            }

            // Set the arrow's rotation only on the Z axis - apply multiplier for visual adjustment
            float adjustedMultiplier = targetIsBehind ? 1 : multiplier;
            arrow.localEulerAngles = new Vector3(0, 0, angleDegrees);
        }
    }

    public void SetCompasssTarget(Transform target)
    {
        compassTarget = target;
    }
}
