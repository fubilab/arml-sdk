using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Manages the UI representation of Task progress in a Level, updating the UI elements as Tasks progress.
    /// </summary>
    public class TaskProgressCanvas : MonoBehaviour
    {
        [SerializeField] private TaskProgressUIContainer progressUIContainerPrefab;
        [SerializeField] private bool showAllTasksAtStart;

        [Serializable]
        private struct TaskNameLanguage
        {
            public string taskName;
            public Language language;
            public string overridenName;
        }

        [SerializeField] private List<TaskNameLanguage> overridenTaskNames;

        private Dictionary<string, TaskProgressUIContainer> taskProgressContainers = new Dictionary<string, TaskProgressUIContainer>();
        private LevelController levelController;

        public static TaskProgressCanvas Instance { get; private set; }

        private void OnEnable()
        {
            Level.OnTaskProgressed += UpdateUI;
            Level.OnLevelStartedAction += PopulateTaskList;
        }

        private void OnDisable()
        {
            Level.OnTaskProgressed -= UpdateUI;
            Level.OnLevelStartedAction -= PopulateTaskList;
        }

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
        /// Initializes the task progress UI elements for each task in the current level.
        /// </summary>
        void Start()
        {
            levelController = LevelController.Instance;
            if (levelController == null)
            {
                Debug.LogWarning("No LevelController in the scene. Add one with a Level for TaskProgressCanvas to work.");
                return;
            }

            PopulateTaskList();
        }

        private void PopulateTaskList()
        {
            if (levelController == null)
                levelController = LevelController.Instance;

            List<LevelTask> tasksList = showAllTasksAtStart ? levelController.GetAllLevelsTasks() : levelController.currentLevel.tasksToComplete;

            foreach (LevelTask task in tasksList)
            {
                if (!task.toBeDisplayed) return;

                //If Task already displayed, return
                if (taskProgressContainers.ContainsKey(task.taskName)) return;

                TaskProgressUIContainer container = Instantiate(progressUIContainerPrefab, this.gameObject.transform);

                string taskName = task.taskName;

                foreach (TaskNameLanguage t in overridenTaskNames)
                {
                    if (task.taskName == t.taskName && t.language == LanguageController.Instance.currentLanguage)
                        taskName = t.overridenName;
                }

                container.TaskProgressName = taskName;

                container.UpdateUI(task.totalProgressIndex, task.CurrentProgressIndex);
                taskProgressContainers[task.taskName] = container;
            }
        }

        public void ResetTaskListLanguage()
        {
            List<LevelTask> tasksList = showAllTasksAtStart ? levelController.GetAllLevelsTasks() : levelController.currentLevel.tasksToComplete;

            foreach (LevelTask task in tasksList)
            {
                if (!task.toBeDisplayed) return;

                string taskName = task.taskName;

                foreach (TaskNameLanguage t in overridenTaskNames)
                {
                    if (task.taskName == t.taskName && t.language == LanguageController.Instance.currentLanguage)
                        taskName = t.overridenName;

                }

                taskProgressContainers[task.taskName].TaskProgressName = taskName;
                taskProgressContainers[task.taskName].UpdateUI(task.totalProgressIndex, task.CurrentProgressIndex);
            }
        }

        /// <summary>
        /// Updates the UI for a specific task when its progress changes.
        /// </summary>
        /// <param name="taskToUpdate">The task that has progressed and needs its UI updated.</param>
        void UpdateUI(LevelTask taskToUpdate)
        {
            if (taskProgressContainers.TryGetValue(taskToUpdate.taskName, out TaskProgressUIContainer container))
            {
                container.UpdateUI(taskToUpdate.totalProgressIndex, taskToUpdate.CurrentProgressIndex);
            }
            else
            {
                Debug.LogWarning($"TaskProgressUIContainer for task '{taskToUpdate.taskName}' not found.");
            }
        }
    }
}