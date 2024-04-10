using ARML.GameBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using UltEvents;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls and manages the Levels in the game, including their activation and progression.
/// </summary>
public class LevelController : MonoBehaviour
{
    [SerializeField] private List<Level> levels = new List<Level>();
    [SerializeField, ReadOnly] public Level currentLevel;

    private int currentLevelIndex;

    public static LevelController Instance { get; private set; }

    private void Awake()
    {
#if UNITY_EDITOR
        CheckForLogicScene();
#endif

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy the GameObject if an instance already exists
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optionally make it persistent
        }

        InitializeLevels();
        ActivateLevel(0); // Start with the first level
    }

    /// <summary>
    /// Initializes all levels contained as children of this GameObject.
    /// </summary>
    private void InitializeLevels()
    {
        foreach (Transform child in transform)
        {
            var level = child.GetComponent<Level>();
            if (level != null)
            {
                levels.Add(level);
                level.levelIndex = levels.IndexOf(level);
            }
        }
    }

    /// <summary>
    /// Advances to the next level in the sequence.
    /// </summary>
    public void PlayNextLevel()
    {
        int nextLevel = currentLevelIndex + 1;
        ActivateLevel(nextLevel);
    }

    /// <summary>
    /// Activates a specific level based on a Level object reference.
    /// </summary>
    /// <param name="level">The Level object to be activated.</param>
    public void PlayLevelByReference(Level level)
    {
        int levelIndex = levels.IndexOf(level);
        if (levelIndex != -1)
        {
            ActivateLevel(levelIndex);
        }
    }

    /// <summary>
    /// Activates a specific level based on its index.
    /// </summary>
    /// <param name="index">The index of the level to be activated.</param>
    private void ActivateLevel(int index)
    {
        if (index >= 0 && index < levels.Count)
        {
            currentLevelIndex = index;
            currentLevel = levels[currentLevelIndex];
            currentLevel.PlayTimeline();

            Debug.Log($"Started playing Level {index + 1}.");

            foreach (var level in levels)
            {
                if (level != currentLevel)
                {
                    level.PauseTimeline();
                }
            }
        }
        else
        {
            Debug.LogError($"Level {index + 1} is not available in the LevelController list.");
        }
    }

    /// <summary>
    /// Handles input for debugging purposes, such as quick level changes.
    /// </summary>
    private void Update()
    {
        HandleDebugInput();
    }

    /// <summary>
    /// Manages debug input to allow for quick activation of levels via keyboard shortcuts.
    /// </summary>
    private void HandleDebugInput()
    {
        for (int i = 0; i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                ActivateLevel(i % 10); // Use modulo for looping back to 0
                break; // Avoid multiple level triggers in the same frame
            }
        }
    }

    public void SetCompassTarget(Transform target)
    {
        CompassController.Instance.compassTarget = target;
    }

    /// <summary>
    /// Utility function that automatically adds the Logic scene if a Content scene is played from the Editor by itself. Deactivates loadOnStart on GameController
    /// </summary>
    private void CheckForLogicScene()
    {
        if (!SceneManager.GetSceneByName("Logic").isLoaded)
        {

            SceneManager.LoadScene("Logic", LoadSceneMode.Additive);
        }
    }
}

[Serializable]
public struct LevelEvent
{
    public int levelIndex;
    public UltEvent levelEvent;
}