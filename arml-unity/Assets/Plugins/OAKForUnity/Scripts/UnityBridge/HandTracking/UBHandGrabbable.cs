using UnityEngine;

namespace OAKForUnity
{
    [RequireComponent(typeof(Collider))]
    public class UBHandGrabbable : MonoBehaviour
    {
        [Header("Hand Tracking")]
        public UBHandTracking handTracking;
        public UBHandGesture grabGesture = UBHandGesture.Fist;

        [Header("Physics")]
        public Rigidbody targetRigidbody;

        private int _grabbedHand = -1;
        private Vector3 _grabOffset;
        private bool _wasKinematic;
        private bool _usedGravity;

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

            if (!handTracking.IsHandGesture(_grabbedHand, grabGesture))
            {
                Release();
                return;
            }

            MoveWithHand(_grabbedHand);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryBeginGrab(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryBeginGrab(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (handTracking != null &&
                handTracking.TryGetGestureTriggerHand(other, grabGesture, out int handIndex) &&
                handIndex == _grabbedHand)
            {
                Release();
            }
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

            _grabbedHand = handIndex;
            _grabOffset = transform.position - handPosition;

            if (targetRigidbody != null)
            {
                _wasKinematic = targetRigidbody.isKinematic;
                _usedGravity = targetRigidbody.useGravity;
                targetRigidbody.isKinematic = true;
                targetRigidbody.useGravity = false;
            }
        }

        private void MoveWithHand(int handIndex)
        {
            if (!handTracking.TryGetHandPosition(handIndex, out Vector3 handPosition))
            {
                Release();
                return;
            }

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
                targetRigidbody.isKinematic = _wasKinematic;
                targetRigidbody.useGravity = _usedGravity;
            }

            _grabbedHand = -1;
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
