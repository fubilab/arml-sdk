using System;
using UnityEngine;

namespace ARML.SceneManagement
{
    /// <summary>
    /// Represents a task within a game, keeping track of its name, progress, and completion status.
    /// </summary>
    [Serializable]
    public class LevelTask
    {
        public string taskName;
        public int totalProgressIndex;
        public bool toBeDisplayed;

        private int currentProgressIndex;
        private bool isCompleted;

        /// <summary>
        /// Gets or sets the current progress index of the task.
        /// Setting this property will automatically update the task's completion status based on progress.
        /// </summary>
        public int CurrentProgressIndex
        {
            get { return currentProgressIndex; }
            set
            {
                if (IsCompleted)
                    return;
                currentProgressIndex = value;
                if (currentProgressIndex >= totalProgressIndex)
                {
                    isCompleted = true;
                    Debug.Log($"Task {taskName} completed!");
                }
            }
        }

        /// <summary>
        /// Gets or sets the completion status of the task.
        /// </summary>
        public bool IsCompleted
        {
            get { return isCompleted; }
            set { isCompleted = value; }
        }
    }
}