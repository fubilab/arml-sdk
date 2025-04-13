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

        public Vector3 rotationOffset;

        public bool ReadLauncherSettings;
        private SettingsConfiguration launcherSettings;

        // Pose reset, pose = target->world * (pose_t0.inverse * pose_t1) = _origin * pose_t1
        private Matrix4x4 _origin = Matrix4x4.identity;
        private Pose _currentPose = Pose.FromMatrix(0, Matrix4x4.identity);

        private void OnEnable()
        {
            if (ReadLauncherSettings)
            {
                launcherSettings = SettingsConfiguration.LoadFromDisk();
                rotationOffset = new Vector3(0, 0, launcherSettings.zOffset);
                if (launcherSettings.trackingMode == TrackingMode.ImuOnly)
                {
                    UseVIO = false;
                    UseOrientationFromBNO = true;
                }
                else if (launcherSettings.trackingMode == TrackingMode.VioOnly)
                {
                    UseVIO = true;
                    UseOrientationFromBNO = false;
                }
                else if (launcherSettings.trackingMode == TrackingMode.VioPlusImu)
                {
                    UseVIO = true;
                    UseOrientationFromBNO = true;
                }
            }
            else
            {
                launcherSettings = new SettingsConfiguration();
            }

            if (Vio.SlamConfig != null)
            {
                // Set the origin to the current pose
                _origin = Vio.SlamConfig.SlamToAprilTagTransform;
            }
            else if (Origin != null)
            {
                // Set the origin to the current pose
                _origin = Origin.localToWorldMatrix;
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
            if (launcherSettings.imuOrientation == ImuOrientation.XBackward)
            {
                transform.localEulerAngles = ArduinoController.Instance.bnoEulerAngles;
            }
            else
            {
                transform.localEulerAngles = new Vector3(
                    -1 * ArduinoController.Instance.bnoEulerAngles.x,
                    ArduinoController.Instance.bnoEulerAngles.y,
                    -1 * ArduinoController.Instance.bnoEulerAngles.z
                );
            }
        }

        private void UpdateVIO() {
            VioOutput output = Vio.Output;
            
            // Cannot update pose if no output, or not tracking
            if (output is null) return;

            _currentPose = output.Pose;

            // Pose w.r.t to Origin (after last reset)
            if (Vio.SlamConfig != null) {
                transform.localPosition = output.Pose.Position + _origin.GetPosition();
                transform.localRotation = 
                    _origin.rotation * output.Pose.Orientation * UnityEngine.Quaternion.Euler(rotationOffset);
            }
            else 
            {
                transform.localPosition = _origin.rotation * output.Pose.Position + _origin.GetPosition();
                transform.localRotation = 
                    _origin.rotation * output.Pose.Orientation * UnityEngine.Quaternion.Euler(rotationOffset);
            }
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
