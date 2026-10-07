namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// A decoded native palm-detection result.
    /// </summary>
    public sealed class HandTrackingDetection
    {
        internal HandTrackingDetection(float score, float[] box, float[] keypoints, float landmarkScore, float handedness, int gesture, float[] landmarks, float[] worldLandmarks)
        {
            Score = score;
            Box = box;
            Keypoints = keypoints;
            LandmarkScore = landmarkScore;
            Handedness = handedness;
            Gesture = gesture;
            Landmarks = landmarks;
            WorldLandmarks = worldLandmarks;
        }

        public float Score { get; }
        public float[] Box { get; }
        public float[] Keypoints { get; }
        public float LandmarkScore { get; }
        public float Handedness { get; }
        public int Gesture { get; }
        public float[] Landmarks { get; }
        public float[] WorldLandmarks { get; }
    }

    /// <summary>
    /// The latest decoded palm detections from the shared native camera feed.
    /// </summary>
    public sealed class HandTrackingOutput
    {
        internal HandTrackingOutput(
            long sequenceNumber,
            double timestamp,
            HandTrackingDetection[] detections)
        {
            SequenceNumber = sequenceNumber;
            Timestamp = timestamp;
            Detections = detections;
        }

        public long SequenceNumber { get; }
        public double Timestamp { get; }
        public HandTrackingDetection[] Detections { get; }
    }
}