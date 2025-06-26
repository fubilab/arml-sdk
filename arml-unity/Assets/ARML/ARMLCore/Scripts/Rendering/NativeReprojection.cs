using System;
using System.Runtime.InteropServices;
using UnityEngine;
using SpectacularAI;
using SpectacularAI.DepthAI;
using SpectacularAI.Native;
using UnityEngine.Rendering;

namespace ARML.Rendering
{
    public static class ExternApi
    {
        [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
        public static extern void sai_set_rendered_orientation(double x, double y, double z, double w);

        [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
        public static extern void sai_set_vio_output_handle(IntPtr vioOutputHandle, int cameraId);

        [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
        public static extern void sai_set_rendered_texture(uint textureId);

        [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
        public static extern void sai_reprojection_plugin_event(int eventId);

        // New: Import the function pointer for the render event
        [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
        public static extern IntPtr GetRenderEventFunc();

        // Cache the function pointer
        private static IntPtr _renderEventFuncPtr = IntPtr.Zero;
        public static IntPtr RenderEventFuncPtr
        {
            get
            {
            if (_renderEventFuncPtr == IntPtr.Zero)
                _renderEventFuncPtr = GetRenderEventFunc();
            return _renderEventFuncPtr;
            }
        }

        [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
        public static extern void sai_set_rendered_depth_texture(uint textureId);

        [DllImport(ApiConstants.saiNativeApi, CallingConvention = ApiConstants.saiCallingConvention)]
        public static extern void sai_set_rendered_projection([In] float[] matrix16);
    }

    public class NativeReprojection : MonoBehaviour
    {
        public int CameraId = 0;
        private UnityEngine.Camera _camera;
        private RenderTexture _offscreenRT;
        private bool _reprojectionEnabled = true;
        private bool _reprojectionFrozen = false;

        void Awake()
        {
            #if UNITY_EDITOR
            Debug.LogWarning("NativeReprojectionDemo: Disabled in Unity Editor - native rendering not supported in editor");
            enabled = false;
            #else
            _camera = GetComponent<UnityEngine.Camera>();
            if (_camera == null)
            _camera = UnityEngine.Camera.main;
            if (_camera == null)
            {
            Debug.LogError("NativeReprojectionDemo: No Camera found!");
            return;
            }
            _camera.depthTextureMode = DepthTextureMode.Depth;
            #endif
        }

        void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        void OnEndCameraRendering(ScriptableRenderContext ctx, UnityEngine.Camera cam)
        {
            if (!_reprojectionEnabled) return;

            if (cam.cameraType != CameraType.Game) return;
            var target = cam.activeTexture ?? (RenderTexture)cam.targetTexture;
            if (target == null) return;

            if (!_reprojectionFrozen)
            {
            var texPtr = target.GetNativeTexturePtr();
            if (texPtr != IntPtr.Zero)
            {
                ExternApi.sai_set_rendered_texture((uint)texPtr.ToInt64());
            }

            var depthTexture = Shader.GetGlobalTexture("_CameraDepthTexture");
            if (depthTexture != null)
            {
                var depthTexPtr = depthTexture.GetNativeTexturePtr();
                if (depthTexPtr != IntPtr.Zero)
                {
                ExternApi.sai_set_rendered_depth_texture((uint)depthTexPtr.ToInt64());
                }
            }
            }

            var vioHandle = Vio.Output?.GetNativeHandle();
            if (vioHandle.HasValue && vioHandle.Value != IntPtr.Zero)
            {
            ExternApi.sai_set_vio_output_handle(vioHandle.Value, CameraId);
            }

            GL.IssuePluginEvent(ExternApi.RenderEventFuncPtr, 0);
        }

        void Start()
        {
            if (_camera != null)
            {
            _offscreenRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _offscreenRT.Create();
            _camera.targetTexture = _offscreenRT;
            }

            if (_camera != null)
            {
            Matrix4x4 proj = _camera.projectionMatrix;
            float[] projColMajor = new float[16];
            for (int row = 0; row < 4; ++row)
                for (int col = 0; col < 4; ++col)
                projColMajor[col * 4 + row] = proj[row, col];
            ExternApi.sai_set_rendered_projection(projColMajor);
            }

            fpsTimeLeft = fpsUpdateInterval;
        }

        void OnDestroy()
        {
            if (_offscreenRT != null)
            {
            if (_camera != null && _camera.targetTexture == _offscreenRT)
                _camera.targetTexture = null;
            _offscreenRT.Release();
            Destroy(_offscreenRT);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
            _reprojectionEnabled = !_reprojectionEnabled;
            if (_reprojectionEnabled)
            {
                _camera.targetTexture = _offscreenRT;
            }
            else
            {
                _camera.targetTexture = null;
            }
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
            _reprojectionFrozen = !_reprojectionFrozen;
            }
            PoseProvider.FrozenMode = _reprojectionFrozen; // Disable camera rendering if frozen

            // FPS calculation
            fpsTimeLeft -= Time.deltaTime;
            fpsAccum += Time.timeScale / Time.deltaTime;
            ++fpsFrames;
            if (fpsTimeLeft <= 0f)
            {
            fps = fpsAccum / fpsFrames;
            fpsTimeLeft = fpsUpdateInterval;
            fpsAccum = 0f;
            fpsFrames = 0;
            }
        }

        void OnGUI()
        {
            #if UNITY_EDITOR
            if (!enabled)
            {
                GUI.Box(new Rect(10, 10, 400, 60), "");
                GUI.Label(new Rect(15, 15, 390, 20), "NativeReprojectionDemo: DISABLED IN EDITOR");
                GUI.Label(new Rect(15, 35, 390, 20), "Native rendering only works in builds.");
                return;
            }
            #endif
            
            GUI.Label(new Rect(10, 10, 300, 20), $"Reprojection (R): {(_reprojectionEnabled ? "ON" : "OFF")}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Freeze view (D): {(_reprojectionFrozen ? "ON" : "OFF")}");
            // FPS display
            GUI.Label(new Rect(10, 50, 300, 20), $"FPS: {fps:F1}");
        }

        // FPS calculation fields
        private float fps = 0f;
        private float fpsUpdateInterval = 0.5f;
        private float fpsAccum = 0f;
        private int fpsFrames = 0;
        private float fpsTimeLeft = 0f;

        void LateUpdate()
        {
            if (Vio.Output is null || _reprojectionFrozen)
            return; // No VIO output or frozen, nothing to update
            var unityQuat = Vio.Output.Pose._orientation;
            ExternApi.sai_set_rendered_orientation(unityQuat.x, unityQuat.y, unityQuat.z, unityQuat.w);
        }
    }
}
