using TMPro;
using UnityEngine;


namespace ARML
{
    /// <summary>
    /// Represents a UI container for displaying the progress of a task.
    /// </summary>
    public class TaskProgressUIContainer : MonoBehaviour
    {
        /// <summary>
        /// The name of the task whose progress is being displayed.
        /// </summary>
        public string TaskProgressName;

        /// <summary>
        /// The text component that displays the task's progress.
        /// </summary>
        public TMP_Text ProgressText { get; set; }

        /// <summary>
        /// Updates the UI to reflect the current progress of the task.
        /// </summary>
        /// <param name="totalIndex">The total progress count of the task.</param>
        /// <param name="currentIndex">The current progress count of the task.</param>
        public void UpdateUI(int totalIndex, int currentIndex)
        {
            if (ProgressText == null)
                ProgressText = GetComponent<TMP_Text>();

            ProgressText.text = $"{TaskProgressName}: {currentIndex}/{totalIndex}";
        }
    }
}