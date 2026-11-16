using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ARMLCore.Editor
{
    /// <summary>
    /// Batch mode build of the hand tracking example scene (Assets/ARML/Examples/HandTracking) as a standalone
    /// player, for testing:
    /// Unity -batchmode -nographics -quit -projectPath ... -executeMethod ARMLCore.Editor.HandTrackingExample.BuildPlayer -buildOutput path\app.exe
    /// </summary>
    public static class HandTrackingExample
    {
        public const string ScenePath = "Assets/ARML/Examples/HandTracking/Scenes/HandTrackingExample.unity";

        public static void BuildPlayer()
        {
            string output = GetArgument("-buildOutput");
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("-buildOutput <path to .exe> is required");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[HandTracking] Build {report.summary.result}: {report.summary.outputPath} ({report.summary.totalErrors} errors)");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static string GetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
