using System.IO;
using System.Text;
using UnityEngine;

namespace ARML.AprilTags {
    public static class Utility
    {
        public static string SerializeAprilTags(AprilTag[] aprilTags)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < aprilTags.Length; ++i)
            {
                AprilTag tag = aprilTags[i];
                sb.Append(tag.ToJson());
                if (i < aprilTags.Length - 1)
                {
                    sb.Append(",");
                }
                sb.Append("\n");
            }
            sb.AppendLine("]");

            string json = sb.ToString();
            string filePath = Path.Combine(Application.persistentDataPath, "tags.json");
            File.WriteAllText(filePath, json);
            Debug.Log("[AprilTags] April Tag file written to: " + filePath);
            return filePath;
        }


        private static readonly Matrix4x4 UNITY_WORLD_TO_SPECTACULAR_AI_WORLD = CreateMatrix(
            1, 0, 0, 0,
            0, 0, 1, 0,
            0, 1, 0, 0,
            0, 0, 0, 1);

        private static readonly Matrix4x4 UNITY_APRIL_TAG_TO_SPECTACULAR_AI_APRIL_TAG = CreateMatrix(
            -1, 0, 0, 0,
            0, 0, 1, 0,
            0, -1, 0, 0,
            0, 0, 0, 1);

        private static readonly Matrix4x4 SPECTACULAR_AI_WORLD_TO_UNITY_WORLD = UNITY_WORLD_TO_SPECTACULAR_AI_WORLD.inverse;
        private static readonly Matrix4x4 SPECTACULAR_AI_APRIL_TAG_TO_UNITY_APRIL_TAG = UNITY_APRIL_TAG_TO_SPECTACULAR_AI_APRIL_TAG.inverse;

        public static Matrix4x4 CreateMatrix(
            float m00, float m01, float m02, float m03,
            float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23,
            float m30, float m31, float m32, float m33)
        {
            return new Matrix4x4( // Vectors given by column so the matrix looks transposed here
                new Vector4(m00, m10, m20, m30),
                new Vector4(m01, m11, m21, m31),
                new Vector4(m02, m12, m22, m32),
                new Vector4(m03, m13, m23, m33)
            );
        }

        public static string SerializeMatrix4x4(Matrix4x4 m)
        {
            return
                "[" +
                $"[{m.m00}, {m.m01}, {m.m02}, {m.m03}]," +
                $"[{m.m10}, {m.m11}, {m.m12}, {m.m13}]," +
                $"[{m.m20}, {m.m21}, {m.m22}, {m.m23}]," +
                $"[{m.m30}, {m.m31}, {m.m32}, {m.m33}]" +
                "]";
        }

        public static Matrix4x4 ConvertAprilTagToSpectacularAICoordinates(Matrix4x4 unityTagToUnityWorld)
        {
            // sai_tag->sai_world = sai_tag->unity_tag->unity_world->sai_world =
            // unity_world->sai_world * unity_tag->unity_world * sai_tag->unity_tag
            return UNITY_WORLD_TO_SPECTACULAR_AI_WORLD *
                unityTagToUnityWorld *
                SPECTACULAR_AI_APRIL_TAG_TO_UNITY_APRIL_TAG;
        }
    }
}