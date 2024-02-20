using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UltEvents;
using UnityEngine.Events;

/// <summary>
/// A grouping of game objectives represented as Tasks. It controls the sequence of events that happen during the game, and the current state and goals of the game stage. 
/// If it's on an object that also has a Timeline component, it's responsible for the playback of the timeline, in order to control time-sensitive events.
/// </summary>
public class Level : MonoBehaviour
{
    [Header("Finish")]
    public bool autoFinishOnTasksCompleted = false;
    public bool autoFinishOnTimelineEnd = false;
    public int levelIndex;

    [Header("Tasks")]
    [SerializeField] public List<GameTask> tasksToComplete = new List<GameTask>();

    [Header("Objects")]
    [SerializeField] private bool deactivateChildObjectsIfLevelUnloaded = false;

    private PlayableDirector director;
    private List<GameObject> children = new List<GameObject>();

    public static event Action<GameTask> OnTaskProgressed;
    public static event Action OnLevelStartedAction;

    [Header("Events")]
    [Tooltip("Event triggered when Level starts.")]
    [SerializeField] private UltEvent OnLevelStartedEvent;

    /// <summary>
    /// Initialization logic for the Level, setting up the PlayableDirector and other necessary properties.
    /// </summary>
    private void Awake()
    {
        // Initialize the PlayableDirector and set its properties
        director = GetComponent<PlayableDirector>();
        if (director)
        {
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.Hold;
        }
    }

    /// <summary>
    /// Starts the level, initializes child objects and deactivates them if required.
    /// </summary>
    private void Start()
    {
        // Populate the children list with references to child GameObjects
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }
        SetChildrenActive(false); // Deactivate all children at start
    }

    /// <summary>
    /// Regularly checks for certain conditions, like timeline's end, during the game's update phase.
    /// </summary>
    private void Update()
    {
        // Regularly check if the timeline has reached its end
        CheckIfTimelineHasReachedEnd();
    }

    /// <summary>
    /// Checks if the timeline has reached its end and handles the consequences.
    /// </summary>
    private void CheckIfTimelineHasReachedEnd()
    {
        // Check if the timeline has finished playing
        if (director && director.time >= director.duration && director.playableGraph.IsPlaying())
        {
            Debug.Log($"Level {levelIndex + 1} timeline has ended");
            director.Pause(); // Pause the timeline
            if (autoFinishOnTimelineEnd)
                LevelController.Instance.PlayNextLevel(); // Automatically proceed to the next level
        }
    }

    /// <summary>
    /// Starts the timeline associated with the level.
    /// </summary>
    public void PlayTimeline()
    {
        // Play the timeline from the start
        if (director)
        {
            director.time = 0f;
            director.Play();
        }
        SetChildrenActive(true); // Activate all children

        //Activate Event
        OnLevelStartedEvent?.Invoke();

        //Invoke Internal Action
        OnLevelStartedAction?.Invoke();
    }

    /// <summary>
    /// Resumes the timeline from its current position.
    /// </summary>
    public void ResumeTimeline()
    {
        // Resume playing the timeline
        if (director)
        {
            director.Play();
        }
        SetChildrenActive(true); // Activate all children
    }

    /// <summary>
    /// Stops the timeline and fast-forwards it to the end.
    /// </summary>
    public void StopTimeline()
    {
        // Fast-forward the timeline to the end
        if (director)
        {
            director.Play();
            director.playableGraph.GetRootPlayable(0).SetSpeed(100f);
            StartCoroutine(FastForwardLevel());
        }
    }

    /// <summary>
    /// Resets the timeline to its initial state and pauses it.
    /// </summary>
    public void ResetTimeline()
    {
        // Reset the timeline to the first frame and pause
        if (director)
        {
            director.time = 0f;
            director.Play(); // Force play for one frame to get binding
            director.Evaluate();
            director.Pause();
        }
    }

    /// <summary>
    /// Pauses the timeline and deactivates child objects if configured.
    /// </summary>
    public void PauseTimeline()
    {
        // Pause the timeline and deactivate all children
        SetChildrenActive(false);
        if (director)
        {
            director.Pause();
        }
    }

    /// <summary>
    /// Coroutine to fast-forward the timeline to its end and then pause.
    /// </summary>
    IEnumerator FastForwardLevel()
    {
        // Coroutine to fast-forward the timeline and then pause at the last frame
        while (director && director.time < director.duration - 0.1f)
        {
            yield return new WaitForSeconds(0.1f);
        }
        if (director)
        {
            director.playableGraph.GetRootPlayable(0).SetSpeed(1f);
            director.Pause();
        }
    }

    /// <summary>
    /// Activates or deactivates child GameObjects based on the specified state.
    /// </summary>
    /// <param name="state">The state to set for child objects (true for active, false for inactive).</param>
    private void SetChildrenActive(bool state)
    {
        // Activate or deactivate all child GameObjects based on the state
        if (deactivateChildObjectsIfLevelUnloaded)
        {
            foreach (var child in children)
            {
                child.SetActive(state);
            }
        }
    }

    /// <summary>
    /// Marks a task as completed and triggers associated logic.
    /// </summary>
    /// <param name="completedTask">The task that has been completed.</param>
    public void CompleteTask(GameTask completedTask)
    {
        // Directly set the task as completed
        if (!completedTask.IsCompleted)
        {
            completedTask.IsCompleted = true;
            OnTaskProgressed?.Invoke(completedTask);

            // Check if all tasks are completed
            if (tasksToComplete.All(task => task.IsCompleted) && autoFinishOnTasksCompleted)
            {
                LevelController.Instance.PlayNextLevel();
            }
        }
    }

    /// <summary>
    /// Increments the progress of a specified task.
    /// </summary>
    /// <param name="taskToProgress">The name of the task to progress.</param>
    /// <param name="increaseProgressByAmount">The amount by which to increase the task's progress.</param>
    public void ProgressTask(string taskToProgress, int increaseProgressByAmount = 1)
    {
        GameTask task = tasksToComplete.FirstOrDefault(t => t.taskName == taskToProgress);
        if (task != null)
        {
            task.CurrentProgressIndex += increaseProgressByAmount;
            OnTaskProgressed?.Invoke(task);

            // Check if the task is now completed and all tasks are completed
            if (task.IsCompleted && tasksToComplete.All(t => t.IsCompleted) && autoFinishOnTasksCompleted)
            {
                //If this is the current level, go to next level
                if (LevelController.Instance.currentLevel == this)
                    LevelController.Instance.PlayNextLevel();
            }
        }
        else
        {
            Debug.LogError($"Task with name {taskToProgress} was not found, make sure it's typed correctly in the Level or on the Event that references it");
        }
    }

    /// <summary>
    /// Marks a task as incomplete.
    /// </summary>
    /// <param name="incompleteTask">The task to mark as incomplete.</param>
    public void MarkTaskIncomplete(GameTask incompleteTask)
    {
        // Directly mark the task as incomplete
        if (incompleteTask.IsCompleted)
        {
            incompleteTask.IsCompleted = false;
            // Additional logic (if any) for when a task is marked as incomplete
        }
    }
}
