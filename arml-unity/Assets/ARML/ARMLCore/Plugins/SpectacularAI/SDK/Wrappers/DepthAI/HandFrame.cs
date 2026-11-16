using System;
using UnityEngine;

namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// The 21 hand landmarks of the MediaPipe hand model.
    /// </summary>
    public enum HandJoint
    {
        Wrist = 0,
        ThumbCmc, ThumbMcp, ThumbIp, ThumbTip,
        IndexMcp, IndexPip, IndexDip, IndexTip,
        MiddleMcp, MiddlePip, MiddleDip, MiddleTip,
        RingMcp, RingPip, RingDip, RingTip,
        PinkyMcp, PinkyPip, PinkyDip, PinkyTip
    }

    /// <summary>
    /// Hand poses recognized from the landmarks (the rules of depthai_hand_tracker). A finger counts as open when it
    /// is stretched, and as closed when it is curled. Numbers count the open fingers, the thumb included.
    /// The numeric values come from the native plugin.
    /// </summary>
    public enum HandGesture
    {
        /// <summary>No known pose, for example fingers half curled</summary>
        None = 0,
        /// <summary>Index finger only</summary>
        One = 1,
        /// <summary>Thumb and index finger</summary>
        Two = 2,
        /// <summary>Thumb, index and middle finger</summary>
        Three = 3,
        /// <summary>Index, middle, ring and little finger, thumb closed</summary>
        Four = 4,
        /// <summary>All five fingers open: the open hand</summary>
        Five = 5,
        /// <summary>All fingers closed</summary>
        Fist = 6,
        /// <summary>Thumb out, the other four fingers closed. The orientation of the hand is not checked.</summary>
        ThumbsUp = 7,
        /// <summary>Index and middle finger open, the others closed</summary>
        Peace = 8
    }

    public static class HandGestureExtensions
    {
        /// <summary>Name for the user interface</summary>
        public static string DisplayName(this HandGesture gesture)
        {
            switch (gesture)
            {
                case HandGesture.One: return "One (1)";
                case HandGesture.Two: return "Two (2)";
                case HandGesture.Three: return "Three (3)";
                case HandGesture.Four: return "Four (4)";
                case HandGesture.Five: return "Open hand (5)";
                case HandGesture.Fist: return "Fist";
                case HandGesture.ThumbsUp: return "Thumbs up";
                case HandGesture.Peace: return "Peace";
                default: return "-";
            }
        }
    }

    /// <summary>
    /// One hand in one hand tracking result. All positions are in the coordinates of the color camera
    /// of the OAK-D at the time the image was captured.
    /// </summary>
    public sealed class HandData
    {
        public const int JointCount = 21;

        /// <summary>Slot of the hand in the result (0 or 1). Use Handedness to tell the hands apart.</summary>
        public int Id;

        /// <summary>Confidence of the landmark model, 0..1</summary>
        public float LandmarkScore;

        /// <summary>
        /// Averaged handedness, &gt; 0.5 is classified as the right hand by the model. Which physical hand this is
        /// depends on how the camera is mounted, see HandTracker.SwapHandedness.
        /// </summary>
        public float Handedness;

        public HandGesture Gesture;

        /// <summary>Rotation of the hand in the image (radians), 0 = fingers pointing up in the image</summary>
        public float RoiRotation;

        /// <summary>Size of the user's hand relative to the average hand assumed by the model</summary>
        public float HandScale = 1.0f;

        /// <summary>Depth of the palm measured by stereo in meters, 0 when not measured (IR projector off)</summary>
        public float StereoDepth;

        /// <summary>True when CameraLandmarks are valid</summary>
        public bool Placed;

        /// <summary>Landmarks in the color image, normalized to 0..1, y pointing down</summary>
        public readonly Vector2[] ImageLandmarks = new Vector2[JointCount];

        /// <summary>
        /// Landmarks in meters in Unity camera coordinates (x right, y up, z forward) of the color camera.
        /// </summary>
        public readonly Vector3[] CameraLandmarks = new Vector3[JointCount];

        public bool IsRightHand { get { return Handedness > 0.5f; } }
    }

    /// <summary>
    /// Result of hand tracking for one color camera frame.
    /// </summary>
    public sealed class HandFrame
    {
        /// <summary>Sequence number of the color frame</summary>
        public long Sequence;

        /// <summary>
        /// Capture time of the frame in seconds. Same clock as VioOutput.Pose.Time, so the camera pose at the
        /// time of the frame can be found from the VIO outputs.
        /// </summary>
        public double Timestamp;

        /// <summary>Time from capturing the frame to having this result on the host (ms)</summary>
        public double LatencyMs;

        public HandData[] Hands = new HandData[0];

        // Layout of the native buffer, see sai_depthai_session_get_hand_frame in the native plugin
        internal static HandFrame Parse(long sequence, double timestamp, double latencyMs, float[] buffer, int handCount, int stride)
        {
            var frame = new HandFrame
            {
                Sequence = sequence,
                Timestamp = timestamp,
                LatencyMs = latencyMs,
                Hands = new HandData[handCount]
            };
            for (int i = 0; i < handCount; i++)
            {
                int o = i * stride;
                var hand = new HandData
                {
                    Id = (int)buffer[o],
                    LandmarkScore = buffer[o + 1],
                    Handedness = buffer[o + 2],
                    Gesture = (HandGesture)(int)buffer[o + 3],
                    RoiRotation = buffer[o + 4],
                    HandScale = buffer[o + 5],
                    StereoDepth = buffer[o + 6],
                    Placed = buffer[o + 7] > 0.5f
                };
                int image = o + 8;
                int camera = o + 8 + 63;
                for (int k = 0; k < HandData.JointCount; k++)
                {
                    hand.ImageLandmarks[k] = new Vector2(buffer[image + k * 3], buffer[image + k * 3 + 1]);
                    // native camera frame is x right, y down, z forward (OpenCV), Unity camera is y up
                    hand.CameraLandmarks[k] = new Vector3(
                        buffer[camera + k * 3], -buffer[camera + k * 3 + 1], buffer[camera + k * 3 + 2]);
                }
                frame.Hands[i] = hand;
            }
            return frame;
        }
    }
}
