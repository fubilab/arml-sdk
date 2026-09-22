using UnityEngine;
using UnityEngine.UI;

namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// Displays the latest planar RGB frame received from the native SAI feed.
    /// Enable the shared color feed on Vio before starting the session.
    /// </summary>
    public sealed class ColorFramePreview : MonoBehaviour
    {
        [SerializeField]
        private Vio source;

        [SerializeField]
        private RawImage target;

        [SerializeField]
        private bool flipVertical = true;

        private Texture2D _texture;
        private byte[] _interleavedData;
        private long _lastSequenceNumber = -1;
        private bool _loggedMissingSource;
        private bool _loggedMissingFrame;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<RawImage>();
            }

            if (source == null)
            {
                source = FindFirstObjectByType<Vio>();
            }
        }

        private void Update()
        {
            if (source == null || target == null)
            {
                if (!_loggedMissingSource)
                {
                    Debug.LogWarning("ColorFramePreview requires a Vio source and RawImage target.");
                    _loggedMissingSource = true;
                }

                return;
            }

            ColorFrame frame = source.GetLatestColorFrame();
            if (frame == null)
            {
                if (!_loggedMissingFrame)
                {
                    Debug.LogWarning(
                        "ColorFramePreview has no native color frame. Enable Vio.UseColor before starting the session.");
                    _loggedMissingFrame = true;
                }

                return;
            }

            _loggedMissingFrame = false;
            if (frame.SequenceNumber == _lastSequenceNumber) return;

            int pixelCount = checked(frame.Width * frame.Height);
            if (frame.Width <= 0 || frame.Height <= 0 || frame.Data.Length < pixelCount * 3)
            {
                return;
            }

            EnsureTexture(frame.Width, frame.Height, pixelCount * 3);
            ConvertPlanarRgbToInterleaved(frame.Data, _interleavedData, pixelCount);
            _texture.LoadRawTextureData(_interleavedData);
            _texture.Apply(false, false);
            target.texture = _texture;
            target.uvRect = flipVertical
                ? new Rect(0f, 1f, 1f, -1f)
                : new Rect(0f, 0f, 1f, 1f);
            _lastSequenceNumber = frame.SequenceNumber;
        }

        private void EnsureTexture(int width, int height, int dataSize)
        {
            if (_texture != null && (_texture.width != width || _texture.height != height))
            {
                Destroy(_texture);
                _texture = null;
            }

            if (_texture == null)
            {
                _texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                _texture.wrapMode = TextureWrapMode.Clamp;
                _texture.filterMode = FilterMode.Bilinear;
            }

            if (_interleavedData == null || _interleavedData.Length != dataSize)
            {
                _interleavedData = new byte[dataSize];
            }
        }

        private static void ConvertPlanarRgbToInterleaved(
            byte[] planarData,
            byte[] interleavedData,
            int pixelCount)
        {
            for (int pixelIndex = 0; pixelIndex < pixelCount; ++pixelIndex)
            {
                interleavedData[pixelIndex * 3] = planarData[pixelIndex];
                interleavedData[pixelIndex * 3 + 1] = planarData[pixelCount + pixelIndex];
                interleavedData[pixelIndex * 3 + 2] = planarData[pixelCount * 2 + pixelIndex];
            }
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }
        }
    }
}