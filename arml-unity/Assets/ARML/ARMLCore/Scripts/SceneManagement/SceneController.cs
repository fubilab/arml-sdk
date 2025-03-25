using System;
using FishNet;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ARML.Interaction;

namespace ARML.SceneManagement
{
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

        private void Singleton()
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

        public Action<Scene> OnGameSceneLoaded;
        public Scene LoadedGameScene;

        private void Awake()
        {
            Singleton();
        }

        private void OnEnable()
        {
            if (InstanceFinder.NetworkManager != null) 
            {
                InstanceFinder.SceneManager.OnLoadEnd += NetworkSceneLoaded;
            }
            else
            {
                SceneManager.sceneLoaded += SceneLoaded;
            }
            
        }

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
            yield return SceneManager.UnloadSceneAsync(currentScene);
            Resources.UnloadUnusedAssets();
            StartCoroutine(LoadSceneByName(currentScene));
        }

        /// <summary>
        /// Resets the current scene by reloading it in a single mode.
        /// </summary>
        public void ResetCurrentSceneSingle()
        {
            //Not used right now due to network scene loading changes
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
        public IEnumerator LoadSceneByName(string scene)
        {
            // If Game ScenSceneManagere is already loaded, return
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
                yield return StartCoroutine(FadeToBlack());
            }

            //Load Level
            Debug.Log($"[SceneController] Started loading scene {scene}");

            //Networked version
            if (InstanceFinder.NetworkManager != null) 
            {
                InstanceFinder.SceneManager.LoadGlobalScenes(new FishNet.Managing.Scened.SceneLoadData(scene));
            }
            else
            {
                SceneManager.LoadScene(scene, LoadSceneMode.Additive);   
            }
        }

        /// <summary>
        /// Loads a specific scene by its build index with an additive load mode.
        /// Includes fade in and fade out effects if enabled.
        /// </summary>
        /// <param name="index">The build index of the scene to load.</param>
        public IEnumerator LoadSceneByIndex(int index)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(index);
            print($"scenePath: {scenePath}");
            string sceneName = scenePath.Split(System.IO.Path.DirectorySeparatorChar).Last().Replace(".unity", "");
            print($"sceneName {sceneName}");
            return LoadSceneByName(sceneName);
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Logic")
                return;
            
            PostLoadScene(scene);
        }


        /// <summary>
        /// Logic to run after scene has finished loading (when NetworkManager is active)
        /// </summary>
        private void NetworkSceneLoaded(FishNet.Managing.Scened.SceneLoadEndEventArgs args)
        {
            string loadedScene = "";

            foreach (Scene scene in args.LoadedScenes)
            {
                if (scene.name == "Logic")
                    return;

                loadedScene = scene.name;
            }

            if (loadedScene == "") return;

            PostLoadScene(args.LoadedScenes[0]);
        }

        private void PostLoadScene(Scene scene)
        {
            SceneManager.SetActiveScene(scene);
            LoadedGameScene = scene;
            Debug.Log($"[SceneController] Finished loading scene {scene.name} and set as active");

            //Set Camera transform to Scene Origin 
            FindObjectOfType<CameraParentController>()?.MoveToSceneOrigin();

            if (fadeToBlackBetweenLoads)
                StartCoroutine(FadeBackToGame());

            if (OnGameSceneLoaded != null) 
            {
                OnGameSceneLoaded(scene);
            }
        }

        /// <summary>
        /// Fades black screen back to the game.
        /// </summary>
        private IEnumerator FadeBackToGame()
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

        /// <summary>
        /// Fades screen to black.
        /// </summary>
        public IEnumerator FadeToBlack()
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
    }
}