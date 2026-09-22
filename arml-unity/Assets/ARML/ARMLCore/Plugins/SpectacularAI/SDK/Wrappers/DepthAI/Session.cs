using System;
using System.Runtime.InteropServices;
using SpectacularAI.Native;

namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// VIO session. Should be created via Pipeline::StartSession.
    /// </summary>
    public sealed class Session : IDisposable
    {
        // Native handle to the Session
        private readonly IntPtr _handle;

        // To detect redundant calls to Dispose
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the Session class.
        /// </summary>
        /// <param name="handle">The native handle to the Session.</param>
        public Session(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException(nameof(handle), "Session handle cannot be IntPtr.Zero");
            }

            _handle = handle;
        }

        /// <summary>
        /// Releases the resources associated with the Session object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // No managed resources to release in this case
                }

                ExternApi.sai_depthai_session_release(_handle);

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the Session class.
        /// </summary>
        ~Session()
        {
            Dispose(false);
        }

        /// <summary>
        /// Check if new output is available
        /// </summary>
        /// <returns>True if output available, otherwise false</returns>
        public bool HasOutput() {
            CheckDisposed();
            return ExternApi.sai_depthai_session_has_output(_handle);
        }

        /// <summary>
        /// Get output from the queue.
        /// </summary>
        /// <returns>If available returns vio output. If not, returns null</returns>
        public VioOutput GetOutput()
        {
            CheckDisposed();
            IntPtr vioOutputHandle = ExternApi.sai_depthai_session_get_output(_handle);
            if (vioOutputHandle == IntPtr.Zero) return null;
            return new VioOutput(vioOutputHandle);
        }

        /// <summary>
        /// Wait until new output is available and then return it.
        /// </summary>
        /// <returns>Vio output</returns>
        public VioOutput WaitForOutput()
        {
            CheckDisposed();
            IntPtr vioOutputHandle = ExternApi.sai_depthai_session_wait_for_output(_handle);
            return new VioOutput(vioOutputHandle);
        }

        /// <summary>
        /// Gets the latest RGB frame captured by the shared native feed, if one is available.
        /// </summary>
        public ColorFrame GetLatestColorFrame()
        {
            CheckDisposed();
            IntPtr colorFrameHandle = ExternApi.sai_depthai_session_get_color_frame(_handle);
            if (colorFrameHandle == IntPtr.Zero) return null;

            try
            {
                int dataSize = checked((int)ExternApi.sai_color_frame_get_data_size(colorFrameHandle));
                byte[] data = new byte[dataSize];
                if (dataSize > 0)
                {
                    Marshal.Copy(
                        ExternApi.sai_color_frame_get_data(colorFrameHandle),
                        data,
                        0,
                        dataSize);
                }

                return new ColorFrame(
                    (int)ExternApi.sai_color_frame_get_width(colorFrameHandle),
                    (int)ExternApi.sai_color_frame_get_height(colorFrameHandle),
                    ExternApi.sai_color_frame_get_sequence_number(colorFrameHandle),
                    ExternApi.sai_color_frame_get_timestamp(colorFrameHandle),
                    data);
            }
            finally
            {
                ExternApi.sai_color_frame_release(colorFrameHandle);
            }
        }

        /// <summary>
        /// Gets the latest decoded palm detections, if one is available.
        /// </summary>
        public HandTrackingOutput GetLatestHandTrackingOutput()
        {
            CheckDisposed();
            IntPtr outputHandle = ExternApi.sai_depthai_session_get_hand_tracking_output(_handle);
            if (outputHandle == IntPtr.Zero) return null;

            try
            {
                int count = ExternApi.sai_hand_tracking_output_get_count(outputHandle);
                HandTrackingDetection[] detections = new HandTrackingDetection[count];
                for (int detectionIndex = 0; detectionIndex < count; ++detectionIndex)
                {
                    float[] box = new float[4];
                    for (int valueIndex = 0; valueIndex < box.Length; ++valueIndex)
                    {
                        box[valueIndex] = ExternApi.sai_hand_tracking_output_get_box_value(
                            outputHandle,
                            detectionIndex,
                            valueIndex);
                    }

                    detections[detectionIndex] = new HandTrackingDetection(
                        ExternApi.sai_hand_tracking_output_get_score(outputHandle, detectionIndex),
                        box);
                }

                return new HandTrackingOutput(
                    ExternApi.sai_hand_tracking_output_get_sequence_number(outputHandle),
                    ExternApi.sai_hand_tracking_output_get_timestamp(outputHandle),
                    detections);
            }
            finally
            {
                ExternApi.sai_hand_tracking_output_release(outputHandle);
            }
        }

        /// <summary>
        /// Add an external trigger input. Causes additional output corresponding
        /// to a certain timestamp to be generated.
        /// </summary>
        /// <param name="t">timestamp, monotonic float seconds</param>
        /// <param name="tag">
        /// additonal tag to indentify this particular trigger event.
        /// The default outputs corresponding to input camera frames have a tag 0.
        /// </param>
        public void AddTrigger(double t, int tag)
        {
            CheckDisposed();
            ExternApi.sai_depthai_session_add_trigger(_handle, t, tag);
        }

        /// <summary>
        /// Add external pose information. VIO will correct its estimates to match the pose.
        /// </summary>
        /// <param name="pose">pose of the output coordinates in the external world coordinates</param>
        /// <param name="positionCovariance">position uncertainty as a covariance matrix in the external world coordinates</param>
        /// <param name="orientationVariance">optional orientation uncertainty as variance of angle</param>
        public void AddAbsolutePose(
            Pose pose,
            Matrix3d positionCovariance,
            double orientationVariance = -1)
        {
            CheckDisposed();
            ExternApi.sai_depthai_session_add_absolute_pose(
                _handle, 
                pose, 
                positionCovariance, 
                orientationVariance);
        }

        /// <summary>
        /// Compute RGB camera pose at vio output.
        /// </summary>
        /// <param name="vioOutput">Vio output at which to compute the pose</param>
        /// <returns>RGB camera's pose</returns>
        public CameraPose GetRgbCameraPose(VioOutput vioOutput)
        {
            CheckDisposed();
            IntPtr cameraPoseHandle = ExternApi.sai_depthai_session_get_rgb_camera_pose(
                _handle,
                vioOutput.GetNativeHandle()); 
            return new CameraPose(cameraPoseHandle);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(VioOutput));
            }
        }

        private struct ExternApi
        {   
            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern bool sai_depthai_session_has_output(IntPtr sessionHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern IntPtr sai_depthai_session_get_output(IntPtr sessionHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern IntPtr sai_depthai_session_wait_for_output(IntPtr sessionHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern IntPtr sai_depthai_session_get_color_frame(IntPtr sessionHandle);

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

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern IntPtr sai_depthai_session_get_hand_tracking_output(IntPtr sessionHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern int sai_hand_tracking_output_get_count(IntPtr outputHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern long sai_hand_tracking_output_get_sequence_number(IntPtr outputHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern double sai_hand_tracking_output_get_timestamp(IntPtr outputHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern float sai_hand_tracking_output_get_score(
                IntPtr outputHandle,
                int detectionIndex);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern float sai_hand_tracking_output_get_box_value(
                IntPtr outputHandle,
                int detectionIndex,
                int valueIndex);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern void sai_hand_tracking_output_release(IntPtr outputHandle);
            
            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern void sai_depthai_session_add_trigger(
                IntPtr sessionHandle, 
                double t, 
                int tag);
            
            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern void sai_depthai_session_add_absolute_pose(
                IntPtr sessionHandle, 
                Pose pose, 
                Matrix3d positionCovariance, 
                double orientationVariance);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern IntPtr sai_depthai_session_get_rgb_camera_pose(
                IntPtr sessionHandle,
                IntPtr vioOutputHandle);

            [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
            public static extern void sai_depthai_session_release(IntPtr sessionHandle);
        }
    }
}
