using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosFrame = RosMessageTypes.MagicLantern.FrameCompressedMsg;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class CanvasImageDisplay : MonoBehaviour
{
    void ShowImage(RosFrame imageMessage)
    {
        if (!RosErrorFlagReader.noError)
        {
            return;
        }
        _imageData = imageMessage.data;
    }

    void Start()
    {
        Debug.Log("[CanvasImageDisplay] Start");
        // LoadImage will replace with with incoming image size.
        _texture2D = new Texture2D(640, 480);
        ROSConnection.GetOrCreateInstance().Subscribe<RosFrame>("slam/rgb", ShowImage);
    }

    void FixedUpdate()
    {
        if (_imageData != null)
        {
            _texture2D.LoadImage(_imageData);
            rawImage.texture = _texture2D;
            if (_firstFrame)
            {
                Debug.Log("[CanvasImageDisplay] " + rawImage.texture.width + " x " + rawImage.texture.height);
                _firstFrame = false;
            }
        }
    }

    // public Image image;
    public RawImage rawImage;

    private Texture2D _texture2D;
    private bool _firstFrame = true;
    private byte[] _imageData;
}