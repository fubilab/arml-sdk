using FishNet.Editing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ARMLWelcomeWindow : EditorWindow
{
    private Texture2D logo;
    private static bool subscribed;

    private void Awake()
    {
        //logo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/ARML/ARMLCore/Textures/arml_logo_text.png", typeof(Texture2D));
        logo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/ARML/ARMLCore/Textures/arml_logo.png", typeof(Texture2D));
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        SubscribeToUpdate();
    }

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

    private static void ShowWindowAtStart()
    {
        EditorApplication.update -= ShowWindowAtStart;

        bool shown = EditorPrefs.GetBool("WelcomeWindowOpened", false);
        if (!shown)
        {
            EditorPrefs.SetBool("WelcomeWindowOpened", true);
            ShowWindow();
        }
    }

    [MenuItem("ARML/Welcome Window")]//
    public static void ShowWindow()
    {
        GetWindow<ARMLWelcomeWindow>("ARML Welcome");
    }

    private void OnGUI()
    {
        GUILayout.Label(logo);
        GUILayout.Label("Welcome to the ARML SDK.", EditorStyles.boldLabel);

        GUILayout.Label("Start by opening the Logic Scene", EditorStyles.boldLabel);
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

    private void LoadExampleScene(string path)
    {
        if (EditorSceneManager.sceneCount > 1)
            EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), false);
        Scene loadedScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        EditorSceneManager.SetActiveScene(loadedScene);
    }
}
