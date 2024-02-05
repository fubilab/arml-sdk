using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages scene transitions and operations, including loading scenes and applying fade effects.
/// </summary>
public class SceneController : MonoBehaviour
{
    [SerializeField] private Image fadeToBlackTexture;
    [SerializeField] bool fadeToBlackBetweenLoads;
    [SerializeField] private float fadeDuration;

    #region Singleton
    public static SceneController Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(transform.parent);
        }
    }
    #endregion

    /// <summary>
    /// Initializes the fade texture's transparency at the start of the game.
    /// </summary>
    private void Start()
    {
        Color tempColor = fadeToBlackTexture.color;
        tempColor.a = 0f;
        fadeToBlackTexture.color = tempColor;
    }

    /// <summary>
    /// Resets the current scene asynchronously, unloading and reloading it additively.
    /// </summary>
    public IEnumerator ResetCurrentSceneAdditive()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        //Are the first 2 lines necessary?
        yield return SceneManager.UnloadSceneAsync(currentScene);
        Resources.UnloadUnusedAssets();
        StartCoroutine(LoadSceneByReference(currentScene));
    }

    /// <summary>
    /// Resets the current scene by reloading it in a single mode.
    /// </summary>
    public void ResetCurrentSceneSingle()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Checks for specific user inputs to trigger scene operations like scene reset.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            //StartCoroutine(ResetCurrentSceneAdditive());
            ResetCurrentSceneSingle();
        }
    }

    /// <summary>
    /// Loads a specific scene by its name asynchronously with an additive load mode.
    /// Includes fade in and fade out effects if enabled.
    /// </summary>
    /// <param name="scene">The name of the scene to load.</param>
    public IEnumerator LoadSceneByReference(string scene)
    {
        // If Game Scene is already loaded, return
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == scene)
            {
                Debug.Log($"Scene {scene} already loaded");
                //Set active just in case
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
                yield break;
            }
        }

        //Fade to Black
        if (fadeToBlackBetweenLoads)
        {
            float timeElapsed = 0f;
            Color fadeColor = fadeToBlackTexture.color;
            while (timeElapsed < fadeDuration)
            {
                fadeColor.a = Mathf.Lerp(0f, 1f, timeElapsed / fadeDuration);
                fadeToBlackTexture.color = fadeColor;
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            fadeColor.a = 1f;
            fadeToBlackTexture.color = fadeColor;
        }

        //Load Level
        Debug.Log($"Started loading scene {scene}");
        //yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        ARMLNetworkManager.singleton.ServerChangeScene(scene);
    }

    public IEnumerator FadeBackToGame(string scene)
    {
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene.Replace(".unity", "")));
        Debug.Log($"Finished loading scene {scene} and set as active");

        //Set Camera transform to Scene Origin 
        FindObjectOfType<CameraParentController>()?.MoveToSceneOrigin();

        //Fade back to game
        if (fadeToBlackBetweenLoads)
        {
            float timeElapsed = 0f;
            Color fadeColor = fadeToBlackTexture.color;
            while (timeElapsed < fadeDuration)
            {
                fadeColor.a = Mathf.Lerp(1f, 0f, timeElapsed / fadeDuration);
                fadeToBlackTexture.color = fadeColor;
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            fadeColor.a = 0f;
            fadeToBlackTexture.color = fadeColor;
        }
    }
}
