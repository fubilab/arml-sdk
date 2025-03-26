using UnityEngine;


namespace ARML.AprilTags {
    /// <summary>
    /// Add this component to a placeholder for an AprilTag in your scene. The runtime will automatically
    /// collect all AprilTag components and pass them to the tracking engine. 
    /// Make sure to set the size correctly (see https://github.com/SpectacularAI/docs/blob/main/pdf/april_tag_instructions.pdf).
    /// IMPORTANT: AprilTag must be placed upright (i.e. on a wall) and with no Z-rotation. 
    /// </summary>
    public class AprilTag : MonoBehaviour
    {
        [Tooltip("April tag id (integer)")]
        [field: SerializeField] public int Id { get; private set; }

        [Tooltip("April tag size in meters")]
        [field: SerializeField] public float Size { get; private set; }

        [Tooltip("April tag family: options tag36h11, tag25h9, tag16h5, tagCircle21h7, tagCircle49h12, tagStandard41h12, tagStandard52h13, tagCustom48h12")]
        [field: SerializeField] public string Family { get; private set; }
        
        public Matrix4x4 TagToWorld 
        {
            get
            {
                Vector3 t = transform.position;
                Quaternion q = transform.rotation;
                Matrix4x4 unityTagToUnityWorld = Matrix4x4.TRS(t, q, Vector3.one);
                return Utility.ConvertAprilTagToSpectacularAICoordinates(unityTagToUnityWorld);
            }
        }

        public string ToJson()
        {
            return 
                "   {\n" +
                $"      \"id\": {Id},\n" +
                $"      \"size\": {Size},\n" +
                $"      \"family\": \"{Family}\",\n" +
                $"      \"tagToWorld\": {Utility.SerializeMatrix4x4(TagToWorld)}\n" +
                "   }";
        }
    }
}