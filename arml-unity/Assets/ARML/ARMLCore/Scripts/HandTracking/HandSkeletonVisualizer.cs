using SpectacularAI.DepthAI;
using UnityEngine;

namespace ARML.HandTracking
{
    /// <summary>
    /// Debug visualization of the tracked hands: a sphere per joint and a line per bone.
    /// Put it on any GameObject in the scene, next to a <see cref="HandTracker"/>.
    /// </summary>
    public class HandSkeletonVisualizer : MonoBehaviour
    {
        [Tooltip("Defaults to the HandTracker in the scene")]
        public HandTracker tracker;

        [Tooltip("Joint sphere diameter in meters")]
        public float jointSize = 0.012f;

        [Tooltip("Bone line width in meters")]
        public float boneWidth = 0.004f;

        public Color leftColor = new Color(1f, 0.65f, 0f);
        public Color rightColor = new Color(0.1f, 0.9f, 0.3f);

        [Tooltip("Optional material. When empty a simple unlit material is created.")]
        public Material material;

        private static readonly int[,] Bones =
        {
            {0, 1}, {1, 2}, {2, 3}, {3, 4},
            {0, 5}, {5, 6}, {6, 7}, {7, 8},
            {5, 9}, {9, 10}, {10, 11}, {11, 12},
            {9, 13}, {13, 14}, {14, 15}, {15, 16},
            {13, 17}, {17, 18}, {18, 19}, {19, 20},
            {0, 17}
        };

        private sealed class HandVisual
        {
            public GameObject Root;
            public Transform[] Joints;
            public LineRenderer[] Bones;
        }

        private HandVisual _left;
        private HandVisual _right;

        private void Start()
        {
            if (tracker == null) tracker = HandTracker.Instance != null ? HandTracker.Instance : FindFirstObjectByType<HandTracker>();
            if (material == null)
            {
                // always included in builds
                material = new Material(Shader.Find("Hidden/Internal-Colored"));
            }
            _left = CreateVisual("Left hand", leftColor);
            _right = CreateVisual("Right hand", rightColor);
        }

        private HandVisual CreateVisual(string name, Color color)
        {
            var visual = new HandVisual
            {
                Root = new GameObject(name),
                Joints = new Transform[HandData.JointCount],
                Bones = new LineRenderer[Bones.GetLength(0)]
            };
            visual.Root.transform.SetParent(transform, false);

            var jointMaterial = new Material(material) { color = color };
            for (int i = 0; i < visual.Joints.Length; i++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = ((HandJoint)i).ToString();
                Destroy(sphere.GetComponent<Collider>());
                sphere.GetComponent<MeshRenderer>().sharedMaterial = jointMaterial;
                sphere.transform.SetParent(visual.Root.transform, false);
                visual.Joints[i] = sphere.transform;
            }
            for (int i = 0; i < visual.Bones.Length; i++)
            {
                var go = new GameObject("Bone " + i);
                go.transform.SetParent(visual.Root.transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.sharedMaterial = jointMaterial;
                line.startColor = line.endColor = color;
                visual.Bones[i] = line;
            }
            visual.Root.SetActive(false);
            return visual;
        }

        private void LateUpdate()
        {
            if (tracker == null) return;
            Apply(_left, tracker.Left);
            Apply(_right, tracker.Right);
        }

        private void Apply(HandVisual visual, TrackedHandPose pose)
        {
            if (visual == null) return;
            if (visual.Root.activeSelf != pose.Tracked) visual.Root.SetActive(pose.Tracked);
            if (!pose.Tracked) return;

            for (int i = 0; i < visual.Joints.Length; i++)
            {
                visual.Joints[i].position = pose.Joints[i];
                visual.Joints[i].localScale = Vector3.one * jointSize;
            }
            for (int i = 0; i < visual.Bones.Length; i++)
            {
                LineRenderer line = visual.Bones[i];
                line.startWidth = line.endWidth = boneWidth;
                line.SetPosition(0, pose.Joints[Bones[i, 0]]);
                line.SetPosition(1, pose.Joints[Bones[i, 1]]);
            }
        }
    }
}
