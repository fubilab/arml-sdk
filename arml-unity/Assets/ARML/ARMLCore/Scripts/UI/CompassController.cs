using UnityEngine;
using UnityEngine.UI;

namespace ARML.UI
{
    public class CompassController : MonoBehaviour
    {
        public Transform compassTarget;
        [SerializeField] RectTransform arrow;
        [SerializeField] float multiplier;
        [SerializeField] bool hideXRot;
        [SerializeField] float hideXRotAngle;
        [SerializeField] Image bgImage;

        private Camera cam;
        private bool targetIsBehind;
        private Image compassImage;
        private Image arrowImage;

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
            compassImage = GetComponent<Image>();
            arrowImage = arrow.GetComponent<Image>();
        }

        // Update is called once per frame
        void Update()
        {
            if (cam == null)
            {
                cam = Camera.main;
                return;
            }

            if (compassTarget == null)
            {
                compassImage.enabled = false;
                arrow.gameObject.SetActive(false);
                bgImage.enabled = false;
                return;
            }

            if (hideXRot)
            {
                Color newColor = compassImage.color;
                Color newBgColor = bgImage.color;
                if (cam.transform.eulerAngles.x < hideXRotAngle || cam.transform.eulerAngles.x > 180)
                {
                    newColor.a = 0f;
                    newBgColor.a = 0f;
                }
                else
                {
                    newColor.a = Mathf.Clamp01((cam.transform.eulerAngles.x - hideXRotAngle) / 10);
                    newBgColor.a = Mathf.Clamp01((cam.transform.eulerAngles.x - hideXRotAngle) / 10);
                }

                compassImage.color = newColor;
                arrowImage.color = newColor;
                bgImage.color = newBgColor;
            }

            if (compassTarget != null && arrow != null)
            {
                compassImage.enabled = true;
                arrow.gameObject.SetActive(true);
                bgImage.enabled = true;

                // Calculate world direction vector from the camera to the target
                Vector3 worldDirection = compassTarget.position - cam.transform.position;

                // Transform the world direction into the camera's local space
                Vector3 localDirection = cam.transform.InverseTransformDirection(worldDirection);

                // Ignore the Y component of localDirection
                localDirection.y = 0f;

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
}