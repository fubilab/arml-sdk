using System;
using System.Drawing.Drawing2D;
using ARML.Arduino;
using ARML.Saving;
using ARML.SceneManagement;
using UnityEngine;

namespace SpectacularAI.DepthAI
{
    /// <summary>
    /// Updates pose of the attached GameObject to pose estimated by VIO.
    /// 
    /// In addition, has options to
    /// 1. Reset position and yaw to 'Origin', when 'ResetKey' is pressed.
    /// 2. Predict pose with estimated linear & angular velocity.
    /// 3. Simple pose smoothing. Note: adds delay
    /// </summary>
    public class PoseProvider : MonoBehaviour
    {
        [Tooltip("If enabled, use output from VIO to control pose.")]
        public bool UseVIO;

        [Tooltip("If enabled, use output from BNO IMU sensor to control orientation. Overrides VIO orientation.")]
        public bool UseOrientationFromBNO;
        
        [Tooltip("Position and yaw are set to match this transformation, identity if None")]
        public Transform Origin;


        [Tooltip("Reset on start up")]
        public bool ResetOnStart = false;

        [Tooltip("Pose is predicted assuming constant linear/angular velocity"), Range(0, 0.2f)]
        public float PosePredictDt = 0f;

        [Tooltip("Smooth pose as pose = prevPose.slerp(predictedPose, alpha). Value 1.0 = no smoothing, decreasing adds delay."), Range(0.001f, 1.0f)]
        public float PoseSmoothAlpha = 1.0f;

        public Vector3 rotationOffset;

        public bool ReadLauncherSettings;
        private SettingsConfiguration launcherSettings;

        // Pose reset, pose = target->world * (pose_t0.inverse * pose_t1) = _origin * pose_t1
        private Matrix4x4 _origin = Matrix4x4.identity;
        private Pose _currentPose = Pose.FromMatrix(0, Matrix4x4.identity);

        // Pose smoothing
        private TrackingStatus _prevTrackingStatus = TrackingStatus.INIT;
        private Vector3 _prevSmoothedPosition;
        private UnityEngine.Quaternion _prevSmoothedOrientation;
        
        private IDataService DataService = new JsonDataService();

        private void OnEnable()
        {
            if (ReadLauncherSettings)
            {
                string path = $"{Application.persistentDataPath}/launcherSettings.json";
                launcherSettings = DataService.LoadData<SettingsConfiguration>(path, false);
                rotationOffset = new Vector3(0, 0, launcherSettings.zOffset);
            }
        }

        private void Start()
        {
            RemoteControl.Instance.OnMenuLongPress = ResetPositionAndYaw;

            if (UseOrientationFromBNO) 
            {
                ArduinoController.Instance.ActivateBNO();
            }
        }

        private void Update()
        {
            if (UseVIO)
            {
                UpdateVIO();
            }            
            if (UseOrientationFromBNO)
            {
                UpdateBNO();
            }
        }

        private void UpdateBNO()
        {
            transform.localEulerAngles = ArduinoController.Instance.bnoEulerAngles;
        }

        private void UpdateVIO() {
            VioOutput output = Vio.Output;
            
            // Cannot update pose if no output, or not tracking
            if (output is null) return;
            if (output.Status is not TrackingStatus.TRACKING)
            {
                _prevTrackingStatus = output.Status;
                return;
            }
            
            if (_prevTrackingStatus is not TrackingStatus.TRACKING)
            {
                _prevTrackingStatus = TrackingStatus.TRACKING;
                _prevSmoothedPosition = output.Pose.Position;
                _prevSmoothedOrientation = output.Pose.Orientation;
            }

            _currentPose = output.Pose;
            if (ResetOnStart)
            {
                ResetOnStart = false;
                ResetPositionAndYaw();
            }

            // Pose prediction
            Vector3 predictedPosition = Utility.PredictPosition(output.Pose.Position, output.Velocity, PosePredictDt);
            UnityEngine.Quaternion predictedOrientation = Utility.PredictOrientation(output.Pose.Orientation, output.AngularVelocity, PosePredictDt);

            // Pose smoothing
            _prevSmoothedPosition = Vector3.Lerp(_prevSmoothedPosition, predictedPosition, PoseSmoothAlpha);
            _prevSmoothedOrientation = UnityEngine.Quaternion.Slerp(_prevSmoothedOrientation, predictedOrientation, PoseSmoothAlpha);

            // Pose w.r.t to Origin (after last reset)
            transform.localPosition = _origin.rotation * _prevSmoothedPosition + _origin.GetPosition();

            if (!UseOrientationFromBNO)
                transform.localRotation = 
                    _origin.rotation * _prevSmoothedOrientation * UnityEngine.Quaternion.Euler(rotationOffset);
        }

        private Matrix4x4 GetPositionAndYaw(Matrix4x4 pose)
        {
            return Matrix4x4.TRS(
               pose.GetPosition(),
               UnityEngine.Quaternion.Euler(0, pose.rotation.eulerAngles.y, 0),
               Vector3.one);
        }

        private void ResetPositionAndYaw()
        {
            if (UseVIO)
            {
                Matrix4x4 localToWorld = _currentPose.AsMatrix();
                Matrix4x4 worldToLocalYaw = GetPositionAndYaw(localToWorld.inverse);
                Matrix4x4 originToWorldYaw = Origin ? GetPositionAndYaw(Origin.localToWorldMatrix) : Matrix4x4.identity;
                _origin = originToWorldYaw * worldToLocalYaw;
            }
            if (UseOrientationFromBNO)
            {
                ArduinoController.Instance.ResetOrientation();
            }
        }
    }
}
