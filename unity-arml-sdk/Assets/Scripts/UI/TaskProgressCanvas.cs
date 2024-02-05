using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the UI representation of Task progress in a Level, updating the UI elements as Tasks progress.
/// </summary>
public class TaskProgressCanvas : MonoBehaviour
{
    [SerializeField] private TaskProgressUIContainer progressUIContainerPrefab;
    private Dictionary<string, TaskProgressUIContainer> taskProgressContainers = new Dictionary<string, TaskProgressUIContainer>();
    private LevelController levelController;

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

    /// <summary>
    /// Initializes the task progress UI elements for each task in the current level.
    /// </summary>
    void Start()
    {
        levelController = FindObjectOfType<LevelController>();
        if (levelController == null)
        {
            Debug.LogWarning("No LevelController in the scene. Add one with a Level for TaskProgressCanvas to work.");
            return;
        }

        PopulateTaskList();
    }

    private void PopulateTaskList()
    {
        foreach (GameTask task in levelController.currentLevel.tasksToComplete)
        {
            TaskProgressUIContainer container = Instantiate(progressUIContainerPrefab, this.gameObject.transform);
            container.TaskProgressName = task.taskName;
            container.UpdateUI(task.totalProgressIndex, task.CurrentProgressIndex);
            taskProgressContainers[task.taskName] = container;
        }
    }

    /// <summary>
    /// Updates the UI for a specific task when its progress changes.
    /// </summary>
    /// <param name="taskToUpdate">The task that has progressed and needs its UI updated.</param>
    void UpdateUI(GameTask taskToUpdate)
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
