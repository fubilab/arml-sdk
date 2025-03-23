using System.IO;
using System.Text;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace ARML.AprilTags {
    public class AprilTagSerializer : MonoBehaviour
    {
        [Button]
        void SerializeAprilTags()
        {
            AprilTag[] aprilTags = FindObjectsByType<AprilTag>(FindObjectsSortMode.None);
            string jsonPath = ARML.AprilTags.Utility.SerializeAprilTags(aprilTags);
            EditorUtility.RevealInFinder(jsonPath);
        }
    }
}