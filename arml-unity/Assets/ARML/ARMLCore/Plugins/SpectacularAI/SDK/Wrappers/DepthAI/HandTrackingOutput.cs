namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// A decoded native palm-detection result.
    /// </summary>
    public sealed class HandTrackingDetection
    {
        internal HandTrackingDetection(float score, float[] box)
        {
            Score = score;
            Box = box;
        }

        public float Score { get; }
        public float[] Box { get; }
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