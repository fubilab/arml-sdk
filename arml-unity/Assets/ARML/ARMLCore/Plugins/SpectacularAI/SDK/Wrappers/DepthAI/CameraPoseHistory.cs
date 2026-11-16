using UnityEngine;

namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// Short history of camera poses (camera to Unity world) from the VIO, used to find the pose of the camera at
    /// the capture time of other sensor data, such as the hand tracking frames. Main thread only.
    /// </summary>
    public sealed class CameraPoseHistory
    {
        private struct Sample
        {
            public double Time;
            public Vector3 Position;
            public UnityEngine.Quaternion Rotation;
        }

        private readonly Sample[] _samples;
        private int _count;
        private int _next;

        public CameraPoseHistory(int capacity)
        {
            _samples = new Sample[capacity];
        }

        public int Count { get { return _count; } }

        public void Clear()
        {
            _count = 0;
            _next = 0;
        }

        /// <param name="time">VIO time (seconds). Must be increasing.</param>
        public void Add(double time, Matrix4x4 cameraToWorld)
        {
            if (_count > 0 && time <= Newest().Time) return;
            _samples[_next] = new Sample
            {
                Time = time,
                Position = cameraToWorld.GetPosition(),
                Rotation = cameraToWorld.rotation
            };
            _next = (_next + 1) % _samples.Length;
            if (_count < _samples.Length) _count++;
        }

        private Sample Newest()
        {
            return _samples[(_next - 1 + _samples.Length) % _samples.Length];
        }

        private Sample At(int ageIndex)
        {
            // ageIndex 0 = oldest
            int first = (_next - _count + _samples.Length) % _samples.Length;
            return _samples[(first + ageIndex) % _samples.Length];
        }

        public bool TryGetLatest(out double time, out Matrix4x4 cameraToWorld)
        {
            if (_count == 0)
            {
                time = 0;
                cameraToWorld = Matrix4x4.identity;
                return false;
            }
            Sample s = Newest();
            time = s.Time;
            cameraToWorld = Matrix4x4.TRS(s.Position, s.Rotation, Vector3.one);
            return true;
        }

        /// <summary>
        /// Camera pose at the given time, interpolated between the VIO outputs. Times slightly after the newest
        /// output (up to maxExtrapolation seconds) return the newest pose.
        /// </summary>
        public bool TryGet(double time, out Matrix4x4 cameraToWorld, double maxExtrapolation = 0.2)
        {
            cameraToWorld = Matrix4x4.identity;
            if (_count == 0) return false;

            Sample newest = Newest();
            if (time >= newest.Time)
            {
                if (time - newest.Time > maxExtrapolation) return false;
                cameraToWorld = Matrix4x4.TRS(newest.Position, newest.Rotation, Vector3.one);
                return true;
            }
            Sample oldest = At(0);
            if (time < oldest.Time) return false;

            // binary search for the last sample with Time <= time
            int lo = 0, hi = _count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (At(mid).Time <= time) lo = mid;
                else hi = mid - 1;
            }
            Sample a = At(lo);
            Sample b = At(Mathf.Min(lo + 1, _count - 1));
            double span = b.Time - a.Time;
            float t = span > 1e-9 ? (float)((time - a.Time) / span) : 0f;
            cameraToWorld = Matrix4x4.TRS(
                Vector3.Lerp(a.Position, b.Position, t),
                UnityEngine.Quaternion.Slerp(a.Rotation, b.Rotation, t),
                Vector3.one);
            return true;
        }
    }
}
