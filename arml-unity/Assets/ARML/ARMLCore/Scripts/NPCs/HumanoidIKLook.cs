using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Controls the IK (Inverse Kinematics) for a humanoid character to look at a specified target.
    /// </summary>
    public class HumanoidIKLook : MonoBehaviour
    {
        Animator animator;
        [SerializeField] GameObject target;
        [SerializeField] bool followMainCamera;
        [SerializeField, Range(0, 1)] float lookAtWeight = 1;
        [SerializeField] Vector3 rotationLimit = new Vector3(0.4f, 0.6f, 0.3f);
        [SerializeField, Range(0, 10)] float distanceLimit = 2.6f;
        [SerializeField] float dummyPivotHeightOffset = 1.7f;

        //Dummy pivot
        GameObject objPivot;

        // Start is called before the first frame update
        void Start()
        {
            animator = GetComponent<Animator>();

            //Dummy pivot
            objPivot = new GameObject("DummyPivot");
            objPivot.transform.parent = transform;
            objPivot.transform.localPosition = new Vector3(0, dummyPivotHeightOffset, 0);

            if (followMainCamera)
                target = Camera.main?.gameObject;
        }

        /// <summary>
        /// LateUpdate is called every frame, after all Update functions have been called.
        /// This method updates the look at target and weight based on the character's rotation and distance limits.
        /// </summary>
        void LateUpdate()
        {
            if (followMainCamera)
                target = Camera.main?.gameObject;

            if (!target)
                return;

            objPivot.transform.LookAt(target.transform);

            //Target distance limit
            float distance = Vector3.Distance(objPivot.transform.position, target.transform.position);

            //Target rotation limit
            float pivotRotY = objPivot.transform.localRotation.y;

            if (Mathf.Abs(pivotRotY) > rotationLimit.y || distance > distanceLimit)
            {
                //lookAtWeight = 0f; //Stop tracking
                lookAtWeight = Mathf.Lerp(lookAtWeight, 0, Time.deltaTime * 1.5f); //Stop tracking
            }
            else
            {
                lookAtWeight = Mathf.Lerp(lookAtWeight, 1, Time.deltaTime * 1.5f);
            }
        }

        /// <summary>
        /// Sets the IK properties for the Animator. This is called by the Animator component and applies the look at weight and position.
        /// </summary>
        private void OnAnimatorIK()
        {
            if (!animator || target == null) return;

            animator.SetLookAtWeight(lookAtWeight);
            animator.SetLookAtPosition(target.transform.position);
        }
    }
}