using TMPro;
using UnityEngine;

namespace UnityStandardAssets.Utility
{
    /// <summary>
    /// Calculates and displays the average frames per second (FPS).
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        const float fpsMeasurePeriod = 0.5f;
        private int m_FpsAccumulator = 0;
        private float m_FpsNextPeriod = 0;
        private int m_CurrentFps;
        const string display = "{0}";
        private TextMeshProUGUI m_GuiText;

        Rigidbody rb;

        /// <summary>
        /// Initializes the FPS counter by setting up the GUI text and the next measurement period.
        /// </summary>
        private void Start()
        {
            m_GuiText = GetComponent<TextMeshProUGUI>();
            m_FpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;

            rb = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Updates the FPS count at regular intervals and displays it on the GUI text.
        /// </summary>
        private void Update()
        {
            // measure average frames per second
            m_FpsAccumulator++;
            if (Time.realtimeSinceStartup > m_FpsNextPeriod)
            {
                m_CurrentFps = (int)(m_FpsAccumulator / fpsMeasurePeriod);
                m_FpsAccumulator = 0;
                m_FpsNextPeriod += fpsMeasurePeriod;
                if (m_GuiText != null)
                    m_GuiText.text = string.Format(display, m_CurrentFps);
            }
        }
    }
}
