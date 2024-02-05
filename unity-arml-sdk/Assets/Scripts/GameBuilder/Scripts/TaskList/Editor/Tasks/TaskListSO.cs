using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARML.GameBuilder
{
    [CreateAssetMenu(menuName = "TaskList", fileName = "New Task List")]
    public class TaskListSO : ScriptableObject
    {
        [SerializeField] List<string> tasks = new List<string>();

        /// <summary>
        /// Returns the list of tasks
        /// </summary>
        public List<string> GetTasks()
        {
            return tasks;
        }

        public void AddTask(string savedTask)
        {
            tasks.Add(savedTask);
        }

        /// <summary>
        /// Adds current list of tasks to the Scriptable Object
        /// </summary>
        public void AddTasks(List<string> savedTasks)
        {
            tasks.Clear();
            tasks = savedTasks;
        }
    }

}
