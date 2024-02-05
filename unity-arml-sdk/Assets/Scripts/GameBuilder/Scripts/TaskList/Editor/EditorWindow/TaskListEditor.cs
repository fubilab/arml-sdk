using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ARML.GameBuilder
{
    public class TaskListEditor : EditorWindow
    {
        VisualElement container;
        ObjectField savedTasksObjectField;
        Button loadTasksButton;
        ToolbarSearchField searchBox;
        TextField taskText;
        Button addTaskButton;
        ScrollView taskListScrollView;
        Button saveProgressButton;
        ProgressBar taskProgressBar;
        Label notificationLabel;

        TaskListSO taskListSO;

        [MenuItem("ARML/Task List")]
        public static void ShowWindow()
        {
            TaskListEditor window = GetWindow<TaskListEditor>();
            window.titleContent = new GUIContent("ARML Task List");
            window.minSize = new Vector2(500, 500);
        }

        public void CreateGUI()
        {
            string filePath = FranUtils.GetScriptableObjectFilePath(this);

            container = rootVisualElement;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(filePath.Substring(0, filePath.Length - 2) + "uxml");
            container.Add(visualTree.Instantiate());

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(filePath.Substring(0, filePath.Length - 2) + "uss");
            container.styleSheets.Add(styleSheet);

            savedTasksObjectField = container.Q<ObjectField>("savedTasksObjectField");
            savedTasksObjectField.objectType = typeof(TaskListSO);

            loadTasksButton = container.Q<Button>("loadTasksButton");
            loadTasksButton.clicked += LoadTasks;

            searchBox = container.Q<ToolbarSearchField>("searchBox");
            searchBox.RegisterValueChangedCallback(OnSearchTextChanged);

            taskText = container.Q<TextField>("taskText");
            taskText.RegisterCallback<KeyDownEvent>(AddTask);

            addTaskButton = container.Q<Button>("addTaskButton");
            addTaskButton.clicked += AddTask;

            taskListScrollView = container.Q<ScrollView>("taskList");

            saveProgressButton = container.Q<Button>("saveProgressButton");
            saveProgressButton.clicked += SaveProgress;

            taskProgressBar = container.Q<ProgressBar>("taskProgressBar");

            notificationLabel = container.Q<Label>("notificationLabel");

            UpdateNotifications("Load in a Task Scriptable Object");
        }

        void AddTask()
        {
            if (string.IsNullOrEmpty(taskText.value))
                return;

            taskListScrollView.Add(CreateTask(taskText.value));
            SaveTask(taskText.value);
            taskText.value = "";
            taskText.Focus();
            UpdateProgress();
        }

        void AddTask(KeyDownEvent e)
        {
            if (Event.current.Equals(Event.KeyboardEvent("Return")))
            {
                AddTask();
            }
        }

        private TaskItem CreateTask(string taskText)
        {
            TaskItem taskItem = new TaskItem(taskText);
            taskItem.GetTaskLabel().text = taskText;
            taskItem.GetTaskToggle().RegisterValueChangedCallback((e) => UpdateProgress()); //Intellisense gave this simplified event method overload handler
            return taskItem;
        }

        void LoadTasks()
        {
            taskListSO = savedTasksObjectField.value as TaskListSO;

            if (taskListSO == null)
                return;

            taskListScrollView.Clear();
            List<string> tasks = taskListSO.GetTasks();
            foreach (string task in tasks)
            {
                taskListScrollView.Add(CreateTask(task));
            }

            UpdateProgress();
            UpdateNotifications("Task List loaded correctly. You can now add more tasks.");
        }

        void SaveTask(string task)
        {
            taskListSO.AddTask(task);
            EditorUtility.SetDirty(taskListSO);
            AssetDatabase.SaveAssetIfDirty(taskListSO);
        }

        void SaveProgress()
        {
            if (taskListSO == null)
                return;

            List<string> tasks = new List<string>();

            foreach (TaskItem task in taskListScrollView.Children())
            {
                if (!task.GetTaskToggle().value)
                {
                    tasks.Add(task.GetTaskLabel().text);
                }
            }

            taskListSO.AddTasks(tasks);

            EditorUtility.SetDirty(taskListSO);
            AssetDatabase.SaveAssetIfDirty(taskListSO);

            LoadTasks();
        }

        void UpdateProgress()
        {
            int count = 0;
            int completed = 0;

            foreach (TaskItem task in taskListScrollView.Children())
            {
                count++;

                if (task.GetTaskToggle().value)
                {
                    completed++;
                }
            }

            if (count > 0)
            {
                taskProgressBar.value = completed / (float)count;
            }
            else
            {
                taskProgressBar.value = 1;
            }

            taskProgressBar.title = $"{taskProgressBar.value * 100:F1} %";
        }

        void OnSearchTextChanged(ChangeEvent<string> changeEvent)
        {
            string searchText = changeEvent.newValue.ToUpper();

            foreach (TaskItem task in taskListScrollView.Children())
            {
                string taskText = task.GetTaskLabel().text.ToUpper();

                if (!string.IsNullOrEmpty(searchText) && taskText.Contains(searchText))
                {
                    task.AddToClassList("highlight");
                }
                else
                {
                    task.RemoveFromClassList("highlight");
                }
            }
        }

        void UpdateNotifications(string text)
        {
            notificationLabel.text = text;
        }
    }
}
