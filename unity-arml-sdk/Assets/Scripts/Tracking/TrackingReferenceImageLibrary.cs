using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.VisualScripting;


public class TrackingReferenceImageLibrary : MonoBehaviour
{
    [Serializable]
    struct TrackingReferenceImage
    {
        public Texture2D Image;
        public string ID;
    }

    private const string PLUGIN_NAME = "camera_motion";

    [DllImport(PLUGIN_NAME)]
    private static extern void processImages(IntPtr imageBytes, int imageBytesSize, string bagFileAddress);

    public string[] imageNames;  // Array to hold image names
    public Texture2D[] images;  // Array to hold Unity textures

    [SerializeField] private List<TrackingReferenceImage> trackingReferenceImageList = new List<TrackingReferenceImage>();
    private Dictionary<string, byte[]> imageDictionary = new Dictionary<string, byte[]>();
    //[SerializeField] private Renderer renderer;


    // private void Awake()
    // {
    //     ConvertImagesToByteArrays();
    // }

    public void ConvertImagesToByteArrays()
    {
        foreach (TrackingReferenceImage referenceImage in trackingReferenceImageList)
        {
            Texture2D compressed = referenceImage.Image;
            //Texture2D tex = compressed.DeCompress();
            Texture2D tex = DeCompress(compressed); //If this works you can delete extension method below

            byte[] imageBytes = tex.EncodeToPNG();
            string imageName = referenceImage.ID;

            IntPtr imageBytesPtr = Marshal.AllocHGlobal(imageBytes.Length);
            Marshal.Copy(imageBytes, 0, imageBytesPtr, imageBytes.Length);

            processImages(imageBytesPtr, imageBytes.Length, imageName);

            Marshal.FreeHGlobal(imageBytesPtr);
        }

        //Load back into texture

        //Texture2D convertedTexture = new Texture2D(256, 128, trackingReferenceImageList[0].Image.format, false);
        //convertedTexture.LoadRawTextureData(imageDictionary["bat"]);
        //convertedTexture.Apply();
        //renderer.material.mainTexture = convertedTexture;
    }

    private Texture2D DeCompress(Texture2D source)
    {
        RenderTexture renderTex = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }
}

//public static class ExtensionMethod
//{
//    public static Texture2D DeCompress(this Texture2D source)
//    {
//        RenderTexture renderTex = RenderTexture.GetTemporary(
//                    source.width,
//                    source.height,
//                    0,
//                    RenderTextureFormat.Default,
//                    RenderTextureReadWrite.Linear);

//        Graphics.Blit(source, renderTex);
//        RenderTexture previous = RenderTexture.active;
//        RenderTexture.active = renderTex;
//        Texture2D readableText = new Texture2D(source.width, source.height);
//        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
//        readableText.Apply();
//        RenderTexture.active = previous;
//        RenderTexture.ReleaseTemporary(renderTex);
//        return readableText;
//    }
//}