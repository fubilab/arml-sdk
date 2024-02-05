using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosFrame = RosMessageTypes.MagicLantern.FrameCompressedMsg;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;


public class ImageDisplay : MonoBehaviour
{
    private Texture2D _texture2d;

    void Start()
    {
        // Create a texture. Texture size does not matter, since
        // LoadImage will replace with with incoming image size.
        _texture2d = new Texture2D(320, 240);
        ROSConnection.GetOrCreateInstance().Subscribe<RosFrame>("image", ShowImage);

    }

    void ShowImage(RosFrame imageMessage)
    {
        
        _texture2d.LoadImage(imageMessage.data);
        GetComponent<Renderer>().material.mainTexture = _texture2d;
    }
}