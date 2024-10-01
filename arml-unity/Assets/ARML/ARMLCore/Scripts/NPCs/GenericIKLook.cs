using System.Collections;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Controls the IK (Inverse Kinematics) for looking at a target in a generic rig.
    /// </summary>
    public class GenericIKLook : MonoBehaviour
    {
        [SerializeField] private Transform bone;
        [SerializeField] private Transform target;
        [SerializeField] private float rotationLimit = 60f;
        [SerializeField] private float lerpDuration;
        [SerializeField] private Vector3 rotationOffset;
        private Quaternion startBoneRotation;
        private Vector3 startParentRotation;
        private bool lerpRunning;
        private bool firstFrameOverLimit;

        private Vector3 direction;
        private Vector3 lookRotation;

        void Start()
        {
            if (target == null)
                return;

            startBoneRotation = bone.rotation;
            startParentRotation = transform.eulerAngles;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            //If currently lerping, let it finish
            if (lerpRunning) return;

            //Calculate angle between agent body transform and target object
            float yRotationDiff = GetYRotationToTarget(transform.gameObject, target.gameObject);

            //If currently under limit, update look rotation, otherwise stay frozen
            if (Mathf.Abs(yRotationDiff) < rotationLimit)
            {
                direction = (target.position - bone.position).normalized;

                //Remap to invert X axis
                //lookRotation = new Vector3(-lookRotation.x, lookRotation.y, lookRotation.z);
            }
            else
            {
                bone.localEulerAngles = yRotationDiff < 0 ?
                    new Vector3(60.53f, 79.61f, 52.07f) : new Vector3(300.34f, 276.89f, 59.70f);

                return;
            }

            lookRotation = (Quaternion.LookRotation(direction) * startBoneRotation).eulerAngles;

            //Account for initial rotation
            lookRotation = lookRotation - startParentRotation;
            lookRotation = new Vector3(bone.eulerAngles.x, lookRotation.y, bone.eulerAngles.z); //Limit Rotation to Y Axis

            bone.eulerAngles = lookRotation;
        }

        /// <summary>
        /// Lerps the rotation of the bone towards a target rotation over a duration.
        /// </summary>
        /// <param name="targetRotation">The target rotation to lerp towards.</param>
        /// <param name="duration">The duration over which to lerp.</param>
        /// <param name="isLocal">Whether the rotation should be applied locally or globally.</param>
        IEnumerator LerpRotation(Quaternion targetRotation, float duration, bool isLocal)
        {
            float time = 0;
            Quaternion startRotation = Quaternion.identity;
            if (isLocal)
                startRotation = bone.localRotation;
            else
                startRotation = bone.rotation;

            while (time < duration)
            {
                if (isLocal)
                    bone.localRotation = Quaternion.Lerp(startRotation, targetRotation, time / duration);
                else
                    bone.rotation = Quaternion.Lerp(startRotation, targetRotation, time / duration);

                time += Time.deltaTime;
                yield return null;
            }
            if (isLocal)
                bone.localRotation = targetRotation;
            else
                bone.rotation = targetRotation;
            lerpRunning = false;
        }

        /// <summary>
        /// Calculates the Y rotation angle needed for the source to look at the target.
        /// </summary>
        /// <param name="source">The source GameObject.</param>
        /// <param name="target">The target GameObject.</param>
        /// <returns>The Y rotation in degrees to face the target.</returns>
        public static float GetYRotationToTarget(GameObject source, GameObject target)
        {
            // Get the local positions of the source and target with respect to their common parent.
            Vector3 localSourcePos = source.transform.InverseTransformPoint(target.transform.position);

            // Calculate the local rotation in degrees.
            float localYRotation = Mathf.Atan2(localSourcePos.x, localSourcePos.z) * Mathf.Rad2Deg;

            return localYRotation;
        }

        public void SetLookTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}