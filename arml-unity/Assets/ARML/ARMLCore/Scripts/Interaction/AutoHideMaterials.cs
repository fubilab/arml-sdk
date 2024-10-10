using UnityEngine;

namespace ARML.Interaction
{
    /// <summary>
    /// Component that makes an object's materials transparent when obstructing the camera view.
    /// The materials will return to their original state when the obstruction is no longer present.
    /// </summary>
    public class AutoHideMaterials : MonoBehaviour
    {
        /// <summary>
        /// The target transform that this component will check for obstructing views.
        /// </summary>
        public Transform targetTransform;

        private Camera mainCamera;
        private Material[] originalMaterials;
        private Renderer[] renderers;

        [SerializeField, Tooltip("The alpha value for transparency when materials are made transparent.")]
        private float transparentAlpha = 0.5f;

        [SerializeField, Tooltip("Maximum angle (in degrees) to consider the object as obstructing the view.")]
        private float maxAngle = 10f;

        [SerializeField, Tooltip("Maximum distance to the camera-target line to consider the object as obstructing the view.")]
        private float maxDistanceToLine = 1f;

        private bool alreadyTransparent;

        /// <summary>
        /// Initializes the component and stores original materials.
        /// </summary>
        void Start()
        {
            mainCamera = Camera.main;
            renderers = GetComponentsInChildren<Renderer>();
            StoreOriginalMaterials();
        }

        /// <summary>
        /// Checks each frame if the target is obstructed and updates material transparency accordingly.
        /// </summary>
        void Update()
        {
            if (!targetTransform) return;

            if (IsObstructingView() && !alreadyTransparent)
            {
                MakeMaterialsTransparent();
            }
            else if (!IsObstructingView() && alreadyTransparent)
            {
                RestoreMaterials();
            }
        }

        /// <summary>
        /// Determines if this object is obstructing the view to the target.
        /// </summary>
        /// <returns>True if the object is obstructing the view; otherwise, false.</returns>
        bool IsObstructingView()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                return false;
            }

            Vector3 cameraToObject = transform.position - mainCamera.transform.position;
            Vector3 cameraToTarget = targetTransform.position - mainCamera.transform.position;

            // Check if the object is within the maximum angle of obstruction
            float angle = Vector3.Angle(cameraToTarget, cameraToObject);
            if (angle > maxAngle) return false;

            // Check if the object is close to the line of sight
            float distanceToLine = Vector3.Cross(cameraToTarget.normalized, cameraToObject.normalized).magnitude;
            return distanceToLine < maxDistanceToLine;
        }

        /// <summary>
        /// Stores the original materials of the object's renderers for restoration.
        /// </summary>
        void StoreOriginalMaterials()
        {
            var materialsList = new System.Collections.Generic.List<Material>();

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    materialsList.Add(mat);
                }
            }

            originalMaterials = materialsList.ToArray();
        }

        /// <summary>
        /// Makes the materials transparent by modifying their properties.
        /// </summary>
        public void MakeMaterialsTransparent()
        {
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                alreadyTransparent = true;
                originalMaterials[i].EnableKeyword("_ALPHAPREMULTIPLY_ON");
                originalMaterials[i].SetFloat("_Surface", 1.0f);
                Color color = originalMaterials[i].color;
                color.a = transparentAlpha;
                originalMaterials[i].color = color;
                originalMaterials[i].renderQueue = 3000; // Set render queue for transparent materials
            }
        }

        /// <summary>
        /// Restores the original materials to their opaque state.
        /// </summary>
        public void RestoreMaterials()
        {
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                alreadyTransparent = false;
                originalMaterials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON"); // Ensure the transparency keyword is disabled
                originalMaterials[i].SetFloat("_Surface", 0.0f);
                Color color = originalMaterials[i].color;
                color.a = 1f; // Restore alpha to opaque
                originalMaterials[i].color = color;
                originalMaterials[i].renderQueue = 3000; // Reset render queue to original
            }
        }
    }
}
