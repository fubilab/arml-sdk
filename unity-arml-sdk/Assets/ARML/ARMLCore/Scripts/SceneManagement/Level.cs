using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UltEvents;
using UnityEngine;
using UnityEngine.Playables;

namespace ARML
{
    /// <summary>
    /// A grouping of game objectives represented as Tasks. It controls the sequence of events that happen during the game, and the current state and goals of the game stage. 
    /// If it's on an object that also has a Timeline component, it's responsible for the playback of the timeline, in order to control time-sensitive events.
    /// </summary>
    public class Level : MonoBehaviour
    {
        [Header("Finish")]
        public bool autoAdvanceOnTasksCompleted = false;
        public bool autoFinishOnTimelineEnd = false;
        public int levelIndex;

        public float autoAdvanceAfterSeconds = 0f;

        [Header("Tasks")]
        [SerializeField] public List<LevelTask> tasksToComplete = new List<LevelTask>();

        [Header("Objects")]
        [SerializeField] private bool deactivateChildObjectsIfLevelUnloaded = false;

        private PlayableDirector director;
        private List<GameObject> children = new List<GameObject>();

        public static event Action<LevelTask> OnTaskProgressed;
        public static event Action OnLevelStartedAction;

        [Header("Events")]
        [Tooltip("Event triggered when Level starts.")]
        [SerializeField] private UltEvent OnLevelStartedEvent;
        [SerializeField] private UltEvent OnAllTasksCompletedEvent;

        [Tooltip("Events triggered when specific combination of Tasks are completed. " +
            "Useful for handling implicit orders in s non-sequential Levels system. They override the OnLevelStartedAction.")]
        [SerializeField] private List<TaskFilterEvent> taskFilterEvents;


        public bool LevelCompleted { get; private set; }

        private Coroutine onLevelStartedEventInvokeCoroutine;

        private float delayOnNextEventCall = 0f;

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
        public void PlayLevel()
        {
            // Play the timeline from the start
            if (director)
            {
                director.time = 0f;
                director.Play();
            }
            SetChildrenActive(true); // Activate all children

            //Invoke Internal Action
            OnLevelStartedAction?.Invoke();

            //First check if AllTasksCompleted, then trigger and ignore filterEvents or OnLevelStartedEvent
            if (tasksToComplete.Count > 0 && tasksToComplete.All(task => task.IsCompleted))
            {
                OnAllTasksCompletedEvent?.Invoke();
            }
            //Check TaskFilterEvents - if none triggered, invoke OnLevelStartedEvent
            else if (!CheckTaskFilterEvents())
            {
                if (onLevelStartedEventInvokeCoroutine == null)
                    onLevelStartedEventInvokeCoroutine = StartCoroutine(InvokeOnLevelStartedEventCoroutine());
            }

            //Log to CSV Export 
            if (gameObject.name != "NoLevel")
            {
                MonitoredAction monitoredAction = new MonitoredAction()
                {
                    ActionType = MonitoredAction.ActionTypeEnum.STARTED,
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ActionObject = this.gameObject.name
                };

                CSVExport.Instance?.monitoredActions.Add(monitoredAction);
            }

            //Check if AutoAdvanceAfterSeconds
            if (autoAdvanceAfterSeconds > 0)
                StartCoroutine(AutoAdvanceAfterSecondsCoroutine());
        }

        private IEnumerator InvokeOnLevelStartedEventCoroutine()
        {
            for (int i = 0; i < OnLevelStartedEvent.PersistentCallsList.Count; i++)
            {
                if (LevelController.Instance.currentLevel != this)
                {
                    onLevelStartedEventInvokeCoroutine = null;
                    yield break;
                }
                if (delayOnNextEventCall > 0)
                    delayOnNextEventCall = 0;
                OnLevelStartedEvent?.PersistentCallsList.ElementAt(i).Invoke();
                yield return new WaitForSeconds(delayOnNextEventCall);
            }
        }

        public void SetDelayOnNextEventCall(float delay)
        {
            delayOnNextEventCall = delay;
        }

        private IEnumerator AutoAdvanceAfterSecondsCoroutine()
        {
            yield return new WaitForSeconds(autoAdvanceAfterSeconds);
            //Only if current Level
            if (LevelController.Instance.currentLevel == this)
                LevelController.Instance.PlayNextLevel();
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
        public void CompleteTask(LevelTask completedTask)
        {
            // Directly set the task as completed
            if (!completedTask.IsCompleted)
            {
                completedTask.IsCompleted = true;
                OnTaskProgressed?.Invoke(completedTask);

                // Check if all tasks are completed
                if (tasksToComplete.All(task => task.IsCompleted) && autoAdvanceOnTasksCompleted)
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
            LevelTask task = tasksToComplete.FirstOrDefault(t => t.taskName == taskToProgress);
            if (task != null)
            {
                if (task.IsCompleted) return;

                task.CurrentProgressIndex += increaseProgressByAmount;
                OnTaskProgressed?.Invoke(task);

                // ONLY if currently loaded Level - Check if the task is now completed and all tasks are completed
                if (LevelController.Instance.currentLevel != this)
                    return;

                //Log to CSV Export
                MonitoredAction taskMonitoredAction = new MonitoredAction()
                {
                    ActionType = task.IsCompleted ? MonitoredAction.ActionTypeEnum.COMPLETED : MonitoredAction.ActionTypeEnum.PROGRESSED,
                    TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ActionObject = task.taskName
                };

                CSVExport.Instance?.monitoredActions.Add(taskMonitoredAction);

                if (task.IsCompleted && tasksToComplete.All(t => t.IsCompleted))
                {
                    OnAllTasksCompletedEvent?.Invoke();

                    //Log to CSV Export
                    MonitoredAction levelmonitoredAction = new MonitoredAction()
                    {
                        ActionType = MonitoredAction.ActionTypeEnum.COMPLETED,
                        TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ActionObject = this.gameObject.name
                    };

                    CSVExport.Instance?.monitoredActions.Add(levelmonitoredAction);

                    LevelCompleted = true;

                    if (autoAdvanceOnTasksCompleted)
                        //If this is the current level, go to next level
                        if (LevelController.Instance.currentLevel == this)
                            LevelController.Instance.PlayNextLevel();
                }
            }
            else
            {
                Debug.LogError($"Task with name {taskToProgress} was not found, make sure it's typed correctly in the Level or on the Event that references it");
            }

            if (LevelController.Instance.currentLevel != this)
                return;

            // ONLY if currently loaded Level - Check if any TaskFilterEvents are triggered
            CheckTaskFilterEvents();
        }

        /// <summary>
        /// Checks if any TaskFilterEvents have been met, and runs it. Keep in mind it only makes sense for one TaskFilterEvent to trigger at a time.
        /// </summary>
        /// <returns>Are there any TaskFilterEvents being triggered </returns>
        private bool CheckTaskFilterEvents()
        {
            if (taskFilterEvents == null || !taskFilterEvents.Any())
                return false;

            //Use reference to all Levels tasks instead of only current one
            var completedTaskNamesSet = new HashSet<string>(LevelController.Instance.GetAllLevelsTasks()
                .Where(task => task.IsCompleted)
                .Select(task => task.taskName));

            // Filter TaskFilterEvents where all tasks are completed
            var completedTaskFilterEvents = taskFilterEvents
                .Where(taskEvent => new HashSet<string>(taskEvent.TaskNames).IsSubsetOf(completedTaskNamesSet))
                .ToList();

            if (!completedTaskFilterEvents.Any())
                return false;

            // Find the TaskFilterEvent with the maximum number of completed tasks
            var maxTaskEvent = completedTaskFilterEvents
                .OrderByDescending(taskEvent => taskEvent.TaskNames.Count)
                .First();

            maxTaskEvent.Event?.Invoke();
            return true;
        }

        /// <summary>
        /// Marks a task as incomplete.
        /// </summary>
        /// <param name="incompleteTask">The task to mark as incomplete.</param>
        public void MarkTaskIncomplete(LevelTask incompleteTask)
        {
            // Directly mark the task as incomplete
            if (incompleteTask.IsCompleted)
            {
                incompleteTask.IsCompleted = false;
                // Additional logic (if any) for when a task is marked as incomplete
            }
        }
    }

    [Serializable]
    public struct TaskFilterEvent
    {
        public List<string> TaskNames;
        public UltEvent Event;
    }
}