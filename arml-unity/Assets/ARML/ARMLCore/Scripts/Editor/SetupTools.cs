// from: https://discussions.unity.com/t/adding-layer-by-script/407882/20

using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;

public static class SetupTools
{
    static AddRequest Request;
    
    [MenuItem("ARML/Setup Packages")]
    public static Task<bool> SetupPackages()
    {
        Request = Client.Add("com.unity.splines");
        
        while (!Request.IsCompleted)
        {
            Task.Delay(500);
        }

        if (Request.Status == StatusCode.Success)
        {
            Debug.Log("Installed: " + Request.Result.packageId);
            AssetDatabase.Refresh();
            return Task.FromResult(true);
        }
        else
        {
            return Task.FromResult(false);
            
        }
    }
    
    /// <summary>
    /// Sets default render pipeline.
    /// </summary>
    [MenuItem("ARML/Setup Render Pipeline")]
    public static void SetupRenderPipeline()
    {
        RenderPipelineAsset armlRenderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
            "Assets/ARML/ARMLCore/Rendering/ARML_URP_RenderPipelineAsset.asset");
        GraphicsSettings.defaultRenderPipeline = armlRenderPipeline;
        QualitySettings.renderPipeline = armlRenderPipeline;
    }
    
    [MenuItem("ARML/Setup Layers")]
    public static void SetupLayers()
    {
        Debug.Log("Adding Layers.");

        Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

        if (asset != null && asset.Length > 0)
        {
            SerializedObject serializedObject = new SerializedObject(asset[0]);
            SerializedProperty layers = serializedObject.FindProperty("layers");
          
           // Add your layers here, these are just examples. Keep in mind: indices below 6 are the built in layers.
            SetLayerAt(layers,  7, "Grabbable");
            SetLayerAt(layers,  8, "Grabbed");
            SetLayerAt(layers, 9, "Ignore_Grabbables");
            SetLayerAt(layers, 10, "Block_Raycast");
            SetLayerAt(layers, 11, "StencilLayer1");
            SetLayerAt(layers, 12, "CantCollideWithItself");
            SetLayerAt(layers, 13, "Map");
            SetLayerAt(layers, 14, "ParticleCollision");

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
    
    static void AddLayerAt(SerializedProperty layers, int index, string layerName, bool tryOtherIndex = true)
    {
       // Skip if a layer with the name already exists.
       for (int i = 0; i < layers.arraySize; ++i)
       {
           if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
           {
               Debug.Log("Skipping layer '" + layerName + "' because it already exists.");
               return;
           }
       }

       // Extend layers if necessary
       if (index >= layers.arraySize)
           layers.arraySize = index + 1;

       // set layer name at index
       var element = layers.GetArrayElementAtIndex(index);
       if (string.IsNullOrEmpty(element.stringValue))
       {
           element.stringValue = layerName;
           Debug.Log("Added layer '" + layerName + "' at index " + index + ".");
       }
       else
       {
           Debug.LogWarning("Could not add layer at index " + index + " because there already is another layer '" + element.stringValue + "'." );

           if (tryOtherIndex)
           {
               // Go up in layer indices and try to find an empty spot.
               for (int i = index + 1; i < 32; ++i)
               {
                   // Extend layers if necessary
                   if (i >= layers.arraySize)
                       layers.arraySize = i + 1;

                   element = layers.GetArrayElementAtIndex(i);
                   if (string.IsNullOrEmpty(element.stringValue))
                   {
                       element.stringValue = layerName;
                       Debug.Log("Added layer '" + layerName + "' at index " + i + " instead of " + index + ".");
                       return;
                   }
               }

               Debug.LogError("Could not add layer " + layerName + " because there is no space left in the layers array.");
           }
       }
    }
    
    static void SetLayerAt(SerializedProperty layers, int index, string layerName)
    {
       // Extend layers if necessary
       if (index >= layers.arraySize)
           layers.arraySize = index + 1;

       // set layer name at index
       var element = layers.GetArrayElementAtIndex(index);
       if (!string.IsNullOrEmpty(element.stringValue))
       {
           Debug.LogWarning("Replacing existing layer at index " + index + ": " + element.stringValue);
       }
       element.stringValue = layerName;
   }

}