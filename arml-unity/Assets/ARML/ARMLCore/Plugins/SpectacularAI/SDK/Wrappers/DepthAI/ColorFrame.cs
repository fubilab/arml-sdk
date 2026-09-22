namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// A copy of the latest RGB frame from the native shared camera feed.
    /// </summary>
    public sealed class ColorFrame
    {
        internal ColorFrame(int width, int height, long sequenceNumber, double timestamp, byte[] data)
        {
            Width = width;
            Height = height;
            SequenceNumber = sequenceNumber;
            Timestamp = timestamp;
            Data = data;
        }

        public int Width { get; }
        public int Height { get; }
        public long SequenceNumber { get; }
        public double Timestamp { get; }
        public byte[] Data { get; }
    }
}