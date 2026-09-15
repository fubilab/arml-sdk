using UnityEngine;

namespace OAKForUnity
{
    [RequireComponent(typeof(Collider))]
    public class UBHandGrabbable : MonoBehaviour
    {
        [Header("Hand Tracking")]
        public UBHandTracking handTracking;
        public UBHandGesture grabGesture = UBHandGesture.Fist;
        [Tooltip("Consecutive tracked frames with a different gesture before releasing.")]
        public int nonGrabGestureFramesToRelease = 3;
        [Range(0f, 1f)]
        [Tooltip("Minimum landmark confidence required to grab or release from a different gesture.")]
        public float minimumLandmarkScore = 0.7f;

        [Header("Physics")]
        public Rigidbody targetRigidbody;

        private int _grabbedHand = -1;
        private Vector3 _grabOffset;
        private bool _wasKinematic;
        private bool _usedGravity;
        private int _nonGrabGestureFrames;

        private void Awake()
        {
            if (targetRigidbody == null)
            {
                targetRigidbody = GetComponent<Rigidbody>();
            }
        }

        private void LateUpdate()
        {
            if (_grabbedHand < 0 || handTracking == null)
            {
                return;
            }

            if (!handTracking.TryGetHandPosition(_grabbedHand, out Vector3 handPosition))
            {
                _nonGrabGestureFrames = 0;
                return;
            }

            float landmarkScore = handTracking.GetHandLandmarkScore(_grabbedHand);
            if (landmarkScore < minimumLandmarkScore)
            {
                _nonGrabGestureFrames = 0;
                return;
            }

            UBHandGesture detectedGesture = handTracking.GetHandGesture(_grabbedHand);
            if (detectedGesture == grabGesture ||
                detectedGesture == UBHandGesture.None)
            {
                _nonGrabGestureFrames = 0;
                MoveWithHand(handPosition);
                return;
            }

            _nonGrabGestureFrames++;
            MoveWithHand(handPosition);

            if (_nonGrabGestureFrames >= Mathf.Max(1, nonGrabGestureFramesToRelease))
            {
                Release();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryBeginGrab(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryBeginGrab(other);
        }

        private void TryBeginGrab(Collider other)
        {
            if (_grabbedHand >= 0 || handTracking == null)
            {
                return;
            }

            if (!handTracking.TryGetGestureTriggerHand(other, grabGesture, out int handIndex))
            {
                return;
            }

            if (!handTracking.TryGetHandPosition(handIndex, out Vector3 handPosition))
            {
                return;
            }

            if (handTracking.GetHandLandmarkScore(handIndex) < minimumLandmarkScore)
            {
                return;
            }

            _grabbedHand = handIndex;
            _grabOffset = transform.position - handPosition;
            _nonGrabGestureFrames = 0;

            if (targetRigidbody != null)
            {
                _wasKinematic = targetRigidbody.isKinematic;
                _usedGravity = targetRigidbody.useGravity;
                targetRigidbody.isKinematic = true;
                targetRigidbody.useGravity = false;
            }
        }

        private void MoveWithHand(Vector3 handPosition)
        {
            Vector3 targetPosition = handPosition + _grabOffset;
            if (targetRigidbody != null)
            {
                targetRigidbody.MovePosition(targetPosition);
            }
            else
            {
                transform.position = targetPosition;
            }
        }

        private void Release()
        {
            if (targetRigidbody != null)
            {
                targetRigidbody.linearVelocity = Vector3.zero;
                targetRigidbody.angularVelocity = Vector3.zero;
                targetRigidbody.isKinematic = _wasKinematic;
                targetRigidbody.useGravity = _usedGravity;
            }

            _grabbedHand = -1;
            _nonGrabGestureFrames = 0;
        }

        private void OnDisable()
        {
            if (_grabbedHand >= 0)
            {
                Release();
            }
        }
    }
}
