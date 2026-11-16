using System;
using System.Runtime.InteropServices;
using SpectacularAI.Native;

namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// A frame of the color camera of the OAK-D (planar RGB). Only available when the color camera is
    /// enabled, which hand tracking does. Dispose it after use.
    /// </summary>
    public sealed class ColorFrame : IDisposable
    {
        private readonly IntPtr _handle;
        private bool _disposed;

        // Reused between frames, main thread only
        private static byte[] _planar;

        internal ColorFrame(IntPtr handle)
        {
            _handle = handle;
            Width = (int)ExternApi.sai_color_frame_get_width(handle);
            Height = (int)ExternApi.sai_color_frame_get_height(handle);
            SequenceNumber = ExternApi.sai_color_frame_get_sequence_number(handle);
            Timestamp = ExternApi.sai_color_frame_get_timestamp(handle);
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        /// <summary>Sequence number of the frame, same as HandFrame.Sequence of the hand tracking result of this frame</summary>
        public long SequenceNumber { get; private set; }

        /// <summary>Capture time in seconds, same clock as the VIO</summary>
        public double Timestamp { get; private set; }

        /// <summary>
        /// Converts the image to RGBA32, 4 bytes per pixel. With flipY the first row of the buffer is the bottom row
        /// of the image, which is what a Unity Texture2D expects.
        /// </summary>
        /// <param name="rgba">buffer of at least Width * Height * 4 bytes</param>
        public bool CopyToRgba(byte[] rgba, bool flipY = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ColorFrame));
            int pixels = Width * Height;
            if (rgba == null || rgba.Length < pixels * 4) return false;
            IntPtr data = ExternApi.sai_color_frame_get_data(_handle);
            if (data == IntPtr.Zero || ExternApi.sai_color_frame_get_data_size(_handle) < (uint)(pixels * 3)) return false;

            if (_planar == null || _planar.Length < pixels * 3) _planar = new byte[pixels * 3];
            Marshal.Copy(data, _planar, 0, pixels * 3);

            for (int y = 0; y < Height; y++)
            {
                int src = y * Width;
                int dst = (flipY ? Height - 1 - y : y) * Width * 4;
                for (int x = 0; x < Width; x++)
                {
                    rgba[dst] = _planar[src + x];
                    rgba[dst + 1] = _planar[pixels + src + x];
                    rgba[dst + 2] = _planar[2 * pixels + src + x];
                    rgba[dst + 3] = 255;
                    dst += 4;
                }
            }
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            ExternApi.sai_color_frame_release(_handle);
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~ColorFrame()
        {
            if (!_disposed) ExternApi.sai_color_frame_release(_handle);
        }

        private struct ExternApi
        {
            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern uint sai_color_frame_get_width(IntPtr colorFrameHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern uint sai_color_frame_get_height(IntPtr colorFrameHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern long sai_color_frame_get_sequence_number(IntPtr colorFrameHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern double sai_color_frame_get_timestamp(IntPtr colorFrameHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern IntPtr sai_color_frame_get_data(IntPtr colorFrameHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern uint sai_color_frame_get_data_size(IntPtr colorFrameHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern void sai_color_frame_release(IntPtr colorFrameHandle);
        }
    }
}
