using TMPro;
using UnityEngine;

namespace ARML.DebugTools
{
    /// <summary>
    /// Displays the position or rotation vector of a target transform as text using TextMeshPro.
    /// </summary>
    public class DebugDisplayVector : MonoBehaviour
    {
        [SerializeField] Transform targetTransform;
        [SerializeField] bool isPosition;
        TMP_Text m_TextMeshPro;

        /// <summary>
        /// Called before the first frame update. Initializes the TextMeshPro component.
        /// </summary>
        void Start()
        {
            m_TextMeshPro = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// Called once per frame. Updates the displayed text to show the current position or rotation of the target transform.
        /// </summary>
        void Update()
        {
            if (targetTransform == null) return;

            if (isPosition)
                m_TextMeshPro.text = targetTransform?.position.ToString();
            else
                m_TextMeshPro.text = targetTransform?.eulerAngles.ToString();
        }
    }
}