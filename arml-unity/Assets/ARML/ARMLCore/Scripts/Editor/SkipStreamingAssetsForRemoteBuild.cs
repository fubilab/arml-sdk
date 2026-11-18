using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Unity copies all of Assets/StreamingAssets into every build, with no per-profile exclusion.
/// For the Android OSC remote build (a UI-only app) that is hundreds of MB of unused scans and
/// videos, so this temporarily renames the folder to "StreamingAssets~" (which Unity ignores)
/// for the duration of that build and puts it back afterwards.
///
/// Only runs for Android builds whose active Build Profile name contains "OSC".
/// If a build crashes before the restore, the folder is moved back on the next editor
/// script reload (a failed build triggers one).
/// </summary>
[InitializeOnLoad]
public class SkipStreamingAssetsForRemoteBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string ProfileNameKeyword = "OSC";
    private const string Folder = "Assets/StreamingAssets";
    private const string Stash = "Assets/StreamingAssets~";

    public int callbackOrder => int.MinValue;

    static SkipStreamingAssetsForRemoteBuild()
    {
        EditorApplication.delayCall += () => Restore(refresh: true);
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;

        BuildProfile profile = BuildProfile.GetActiveBuildProfile();
        if (profile == null || !profile.name.Contains(ProfileNameKeyword)) return;
        if (!Directory.Exists(Folder)) return;

        if (Directory.Exists(Stash))
        {
            Debug.LogWarning($"[Build] {Stash} already exists; leaving StreamingAssets in place for this build.");
            return;
        }

        try
        {
            Directory.Move(Folder, Stash);
            if (File.Exists(Folder + ".meta")) File.Move(Folder + ".meta", Stash + ".meta");
            Debug.Log($"[Build] Excluded StreamingAssets from build '{profile.name}' (will be restored afterwards).");
        }
        catch (IOException e)
        {
            Debug.LogWarning($"[Build] Could not set StreamingAssets aside, it will be included: {e.Message}");
            Restore(refresh: false);
        }
    }

    public void OnPostprocessBuild(BuildReport report) => Restore(refresh: true);

    private static void Restore(bool refresh)
    {
        if (!Directory.Exists(Stash)) return;

        if (Directory.Exists(Folder))
        {
            Debug.LogWarning($"[Build] Both {Folder} and {Stash} exist; resolve manually (nothing was moved).");
            return;
        }

        Directory.Move(Stash, Folder);
        if (File.Exists(Stash + ".meta")) File.Move(Stash + ".meta", Folder + ".meta");
        if (refresh) AssetDatabase.Refresh();
        Debug.Log("[Build] StreamingAssets restored.");
    }
}
