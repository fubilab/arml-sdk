using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Collection of useful methods for development
/// </summary>
public static class FranUtils 
{
#if UNITY_EDITOR
    /// <summary>
    /// Returns the Assets file path for a Scriptable Object
    /// </summary>
    public static string GetScriptableObjectFilePath(ScriptableObject so)
    {
        MonoScript ms = MonoScript.FromScriptableObject(so);
        return AssetDatabase.GetAssetPath(ms);
    }

    /// <summary>
    /// Returns the Assets file path for an asset
    /// </summary>
    /// <param name="assetName">The asset name.</param>
    /// <returns>The asset path.</returns>
    public static string GetAssetFilePathFromName(string assetName)
    {
        return AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(assetName)[0]);
    }
#endif
}
