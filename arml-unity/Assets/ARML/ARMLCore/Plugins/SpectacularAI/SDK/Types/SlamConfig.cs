using System.IO;
using UnityEngine;

namespace SpectacularAI
{
    public class SlamConfig 
    {
        public float[][] slamToUnity;

        public static SlamConfig ReadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            SlamConfig config = Newtonsoft.Json.JsonConvert.DeserializeObject<SlamConfig>(json);
            return config;
        }

        public static void WriteToFile(string path, SlamConfig config)
        {
            var json = JsonUtility.ToJson(config);
            File.WriteAllText(path, json);
        }

        private Matrix4d ToMatrix4d(float[][] matrix)
        {
            Matrix4d s;
            s.m00 = matrix[0][0];
            s.m01 = matrix[0][1];
            s.m02 = matrix[0][2];
            s.m03 = matrix[0][3];
            s.m10 = matrix[1][0];
            s.m11 = matrix[1][1];
            s.m12 = matrix[1][2];
            s.m13 = matrix[1][3];
            s.m20 = matrix[2][0];
            s.m21 = matrix[2][1];
            s.m22 = matrix[2][2];
            s.m23 = matrix[2][3];
            s.m30 = matrix[3][0];
            s.m31 = matrix[3][1];
            s.m32 = matrix[3][2];
            s.m33 = matrix[3][3];
            return s;
        }

        public Matrix4x4 SlamWorldToUnityWorldMatrix
        {
            get
            {
                Matrix4d matrix = ToMatrix4d(slamToUnity);
                return Utility.SPECTACULAR_AI_WORLD_TO_UNITY_WORLD * 
                    matrix.ToUnity() * 
                    Utility.SPECTACULAR_AI_WORLD_TO_UNITY_WORLD;
            }
        }

    }
}