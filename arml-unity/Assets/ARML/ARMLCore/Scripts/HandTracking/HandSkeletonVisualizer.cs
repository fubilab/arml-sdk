using SpectacularAI.DepthAI;
using UnityEngine;

namespace ARML.HandTracking
{
    /// <summary>What <see cref="HandSkeletonVisualizer"/> draws</summary>
    public enum HandVisualMode
    {
        /// <summary>Nothing</summary>
        Off,
        /// <summary>
        /// Only a small dot at the wrist and each fingertip. Light enough to project onto a hand without looking like
        /// a hand to the camera, which sees the projection.
        /// </summary>
        Dots,
        /// <summary>A sphere per joint and a line per bone</summary>
        Skeleton
    }

    /// <summary>
    /// Debug visualization of the tracked hands: a sphere per joint and a line per bone, or only dots at the wrist and
    /// the fingertips. Put it on any GameObject in the scene, next to a <see cref="HandTracker"/>.
    /// A white dot marks the pinch point while a hand pinches.
    /// </summary>
    public class HandSkeletonVisualizer : MonoBehaviour
    {
        [Tooltip("Defaults to the HandTracker in the scene")]
        public HandTracker tracker;

        [Tooltip("What to draw. F3 cycles through the modes. Dots is meant for tests with a projector: the camera sees " +
            "what is projected onto the hands, and a full skeleton drawn on them can look like a hand to it.")]
        public HandVisualMode mode = HandVisualMode.Skeleton;

        public KeyCode modeKey = KeyCode.F3;

        [Tooltip("Joint sphere diameter in meters (Skeleton mode)")]
        public float jointSize = 0.012f;

        [Tooltip("Dot diameter in meters (Dots mode)")]
        public float dotSize = 0.010f;

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
            public Transform PinchDot;
        }

        private HandVisual _left;
        private HandVisual _right;

        // wrist and the five fingertips: joints 0, 4, 8, 12, 16 and 20
        private static bool IsDot(int joint)
        {
            return joint % 4 == 0;
        }

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

            GameObject pinchDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pinchDot.name = "Pinch point";
            Destroy(pinchDot.GetComponent<Collider>());
            pinchDot.GetComponent<MeshRenderer>().sharedMaterial = new Material(material) { color = Color.white };
            pinchDot.transform.SetParent(visual.Root.transform, false);
            pinchDot.SetActive(false);
            visual.PinchDot = pinchDot.transform;

            visual.Root.SetActive(false);
            return visual;
        }

        private void Update()
        {
            if (modeKey != KeyCode.None && Input.GetKeyDown(modeKey))
            {
                mode = (HandVisualMode)(((int)mode + 1) % 3);
                Debug.Log("[HandTracking] Hand visual: " + mode);
            }
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
            bool show = mode != HandVisualMode.Off && pose.Tracked;
            if (visual.Root.activeSelf != show) visual.Root.SetActive(show);
            if (!show) return;

            bool skeleton = mode == HandVisualMode.Skeleton;
            float size = skeleton ? jointSize : dotSize;
            for (int i = 0; i < visual.Joints.Length; i++)
            {
                bool on = skeleton || IsDot(i);
                if (visual.Joints[i].gameObject.activeSelf != on) visual.Joints[i].gameObject.SetActive(on);
                if (!on) continue;
                visual.Joints[i].position = pose.Joints[i];
                visual.Joints[i].localScale = Vector3.one * size;
            }
            for (int i = 0; i < visual.Bones.Length; i++)
            {
                LineRenderer line = visual.Bones[i];
                if (line.gameObject.activeSelf != skeleton) line.gameObject.SetActive(skeleton);
                if (!skeleton) continue;
                line.startWidth = line.endWidth = boneWidth;
                line.SetPosition(0, pose.Joints[Bones[i, 0]]);
                line.SetPosition(1, pose.Joints[Bones[i, 1]]);
            }

            bool pinching = pose.Gesture == HandGesture.Pinch;
            if (visual.PinchDot.gameObject.activeSelf != pinching) visual.PinchDot.gameObject.SetActive(pinching);
            if (pinching)
            {
                visual.PinchDot.position = pose.PinchPoint;
                visual.PinchDot.localScale = Vector3.one * size * 1.8f;
            }
        }
    }
}
