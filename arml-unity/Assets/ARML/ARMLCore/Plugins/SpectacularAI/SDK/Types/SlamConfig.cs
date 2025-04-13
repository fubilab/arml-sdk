using System.IO;
using UnityEngine;

namespace SpectacularAI
{
    public class SlamConfig 
    {
        public float[] aprilTagCamToWorld;
        public float[] slamCamToWorld;

        private Matrix4x4? cachedSlamToAprilTagTransform = null;

        public static SlamConfig ReadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            Debug.Log("[VIO] SlamConfig JSON: " + json);
            
            SlamConfig config = Newtonsoft.Json.JsonConvert.DeserializeObject<SlamConfig>(json);
            Debug.Log("[VIO] SlamConfig.AprilTagCamToWorld: " + config.AprilTagCamToWorld);
            Debug.Log("[VIO] SlamConfig.SlamCamToWorld: " + config.SlamCamToWorld);
            Debug.Log("[VIO] SlamConfig.SlamToAprilTagTransform: " + config.SlamToAprilTagTransform);
            return config;
        }

        public static void WriteToFile(string path, SlamConfig config)
        {
            var json = JsonUtility.ToJson(config);
            File.WriteAllText(path, json);
        }

        private Matrix4d ToMatrix4d(float[] matrix)
        {
            Matrix4d s;
            s.m00 = matrix[0];
            s.m01 = matrix[1];
            s.m02 = matrix[2];
            s.m03 = matrix[3];
            s.m10 = matrix[4];
            s.m11 = matrix[5];
            s.m12 = matrix[6];
            s.m13 = matrix[7];
            s.m20 = matrix[8];
            s.m21 = matrix[9];
            s.m22 = matrix[10];
            s.m23 = matrix[11];
            s.m30 = matrix[12];
            s.m31 = matrix[13];
            s.m32 = matrix[14];
            s.m33 = matrix[15];
            return s;
        }

        public Matrix4d AprilTagCamToWorld
        {
            get
            {
                return ToMatrix4d(aprilTagCamToWorld);
            }
        }
        public Matrix4d SlamCamToWorld
        {
            get
            {
                return ToMatrix4d(slamCamToWorld);
            }
        }

        public Matrix4x4 SlamToAprilTagTransform 
        {
            get
            {
                if (cachedSlamToAprilTagTransform == null)
                {
                    cachedSlamToAprilTagTransform = 
                        Utility.TransformCameraToWorldMatrixToUnity(AprilTagCamToWorld).inverse * 
                        Utility.TransformCameraToWorldMatrixToUnity(SlamCamToWorld);
                }
                return cachedSlamToAprilTagTransform.Value;
            }
        }

        // public Quaternion SlamToAprilTagRotation 
        // {
        //     get
        //     {
        //         if (cachedSlamToAprilTagRotation == null)
        //         {
        //             cachedSlamToAprilTagRotation = 
        //                 Utility.TransformCameraToWorldQuaternionToUnity(AprilTagCamToWorld).inverse * 
        //                 Utility.TransformCameraToWorldMatrixToUnity(SlamCamToWorld);
        //         }
        //         return cachedSlamToAprilTagTransform.Value;
        //     }
        // }
    }
}