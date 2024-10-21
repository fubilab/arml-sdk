using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor window that displays a welcome screen for the ARML SDK, offering quick access to key scenes and documentation.
/// </summary>
public class ARMLWelcomeWindow : EditorWindow
{
    private Texture2D logo;
    private static bool subscribed;

    /// <summary>
    /// Loads the ARML logo asset when the window is initialized.
    /// </summary>
    private void Awake()
    {
        logo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/ARML/ARMLCore/Textures/arml_logo_text.png", typeof(Texture2D));
    }

    /// <summary>
    /// Initializes the window when the editor loads and subscribes to the update event.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        SubscribeToUpdate();
    }

    /// <summary>
    /// Subscribes the window display function to the EditorApplication update event.
    /// </summary>
    private static void SubscribeToUpdate()
    {
        if (Application.isBatchMode)
            return;

        if (!subscribed && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            subscribed = true;
            EditorApplication.update += ShowWindowAtStart;
        }
    }

    /// <summary>
    /// Displays the welcome window on startup if it hasn't been shown yet.
    /// </summary>
    private static void ShowWindowAtStart()
    {
        EditorApplication.update -= ShowWindowAtStart;

        bool shown = EditorPrefs.GetBool("ARMLWelcomeWindowOpened", false);
        if (!shown)
        {
            EditorPrefs.SetBool("ARMLWelcomeWindowOpened", true);
            ShowWindow();
        }
    }

    /// <summary>
    /// Opens the ARML Welcome Window.
    /// </summary>
    [MenuItem("ARML/Welcome Window")]
    public static void ShowWindow()
    {
        GetWindow<ARMLWelcomeWindow>("ARML SDK Welcome");
    }

    /// <summary>
    /// Displays the GUI content of the welcome window.
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space();
        GUILayout.Label(logo);
        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();

        GUILayout.Label("Welcome to the ARML SDK!", EditorStyles.boldLabel);

        GUILayout.Label("Get started by opening the Logic Scene");
        if (GUILayout.Button("Open Logic Scene"))
        {
            EditorSceneManager.OpenScene("Assets/ARML/ARMLCore/Scenes/Logic.unity");
        }

        GUILayout.Label("Example Scenes", EditorStyles.boldLabel);
        if (GUILayout.Button("Add WallGame Scene"))
        {
            LoadExampleScene("Assets/ARML/Examples/WallGame/Scenes/WallGame.unity");
        }

        if (GUILayout.Button("Add GarumGame Scene"))
        {
            LoadExampleScene("Assets/ARML/Examples/GarumGame/Scenes/GarumGame.unity");
        }

        GUILayout.Label("SDK Docs", EditorStyles.boldLabel);
        if (GUILayout.Button("Open SDK Docs"))
        {
            Application.OpenURL("https://fubilab.github.io/arml-sdk/");
        }
    }

    /// <summary>
    /// Loads the specified example scene in the editor, closing other active scenes if necessary.
    /// </summary>
    /// <param name="path">Path to the scene to be loaded.</param>
    private void LoadExampleScene(string path)
    {
        if (EditorSceneManager.sceneCount > 1)
            EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), false);
        Scene loadedScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        EditorSceneManager.SetActiveScene(loadedScene);
    }
}
