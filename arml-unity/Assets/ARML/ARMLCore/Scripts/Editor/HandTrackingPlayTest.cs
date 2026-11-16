using System;
using UnityEditor;
using UnityEngine;

namespace ARMLCore.Editor
{
    /// <summary>
    /// Batch mode test of the hand tracking example scene in the Unity Editor (Play mode):
    /// Unity -batchmode -nographics -projectPath ... -executeMethod ARMLCore.Editor.HandTrackingPlayTest.Run -playSeconds 25
    /// (without -quit). The VIO and hand tracking log to the Editor log; the process exits with the result.
    /// </summary>
    [InitializeOnLoad]
    public static class HandTrackingPlayTest
    {
        private const string ActiveKey = "ARML.HandTrackingPlayTest.Active";
        private const string SecondsKey = "ARML.HandTrackingPlayTest.Seconds";
        private static double _playStart;
        private static bool _wasPlaying;

        static HandTrackingPlayTest()
        {
            EditorApplication.update += Update;
        }

        public static void Run()
        {
            int seconds = 25;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-playSeconds") int.TryParse(args[i + 1], out seconds);
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HandTrackingExample.ScenePath);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(SecondsKey, seconds);
            Debug.Log($"[HandTrackingPlayTest] entering play mode for {seconds} s");
            EditorApplication.EnterPlaymode();
        }

        private static void Update()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;

            if (EditorApplication.isPlaying)
            {
                if (!_wasPlaying)
                {
                    _wasPlaying = true;
                    _playStart = EditorApplication.timeSinceStartup;
                }
                if (EditorApplication.timeSinceStartup - _playStart > SessionState.GetInt(SecondsKey, 25))
                {
                    Debug.Log("[HandTrackingPlayTest] done, leaving play mode");
                    EditorApplication.ExitPlaymode();
                }
            }
            else if (_wasPlaying)
            {
                SessionState.SetBool(ActiveKey, false);
                Debug.Log("[HandTrackingPlayTest] left play mode, exiting");
                EditorApplication.Exit(0);
            }
        }
    }
}
