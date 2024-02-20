using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraGrabber : MonoBehaviour
{
    //public Rigidbody rb;
    [SerializeField] Transform target; //grabbables will try to attach themselves to this point and not the object root
    [SerializeField] Vector3 targetRotationOffset; //grabbables will try to attach themselves to this point and not the object root
    private Grabbable grabbedObject;
    private Grabbable pendingGrabbedObject;
    private Grabbable lastGrabbedObject;
    private bool canGrabLastGrabbedObject;

    [Tooltip("Minimum separation distance between the placed object and the hand to be able to grab it again right after placing it (to prevent instant grabing right after placement)")]
    public float minimumSquaredDistanceToRegrabPlacedObject = 0.1f;
    private List<Grabbable> grabbablesInsideTrigger = new List<Grabbable>();
    public Camera cam { get; private set; }

    [Header("Audio and feedback")]
    public ActionFeedback feedback;

    private float targetDistanceToCam;

    private DebugCanvasController debugCanvasController;

    public void Start()
    {
        cam = Camera.main;
        //target = new GameObject("Grab Target").transform;
        target.transform.parent = transform;
        targetDistanceToCam = 1f;
        target.transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z + targetDistanceToCam);
        target.transform.localEulerAngles = targetRotationOffset;

        //This is horrible but due to the Camera Grab Collider's rotation (because of lens shift) - we need to apply an offset to target rotation
        //target.transform.Rotate(new Vector3(transform.eulerAngles.x, 0, 0));

        debugCanvasController = FindObjectOfType<DebugCanvasController>();
    }

    private void GrabObject()
    {
        //Set target to grabbed object distance -- move this to its own function?

        //If Grabbable wants to override the distance to cam, else stay at the current distance (default behavior)
        //if (pendingGrabbedObject.OverrideDistanceToCam)
        //    targetDistanceToCam = pendingGrabbedObject.DistanceToCam;
        //else

        targetDistanceToCam = Vector3.Distance(pendingGrabbedObject.transform.position, transform.position);

        target.localPosition = new Vector3(0, 0, targetDistanceToCam);

        ForceGrabObject(pendingGrabbedObject);
        grabbedObject.iTimer.OnFinishInteraction -= GrabObject; //Unsubscribe self
        pendingGrabbedObject = null;
    }

    public void ForceGrabObject(Grabbable other)
    {
        grabbedObject = other;
        grabbedObject.OnPlace += ForceReleaseObject;
        feedback?.PlayRandomTriggerFeedback();
        other.Grab();
    }

    // Update is called once per frame
    private void ReleaseObject()
    {
        if (grabbedObject != null) //Check for forced releases, it might not be grabbing
            grabbedObject.Release();
        ForceReleaseObject();
    }

    public void ForceReleaseObject()
    {
        //Clear the placement subscription to avoid bugs
        if (grabbedObject != null)
            grabbedObject.OnPlace -= ForceReleaseObject;

        pendingGrabbedObject = null;
        lastGrabbedObject = grabbedObject;
        grabbedObject = null;

        canGrabLastGrabbedObject = false;
    }

    private void FixedUpdate()
    {
        if (grabbedObject != null)
        {
            grabbedObject.UpdateGrabbedPosition();
        }

        if (!canGrabLastGrabbedObject)
            CheckIfCanGrabLastGrabbedObject();

        if (pendingGrabbedObject == null)
            return;

        //This is not necessary if we are not using modified mesh colliders
        
        ////Search for the pending grabbed
        //Grabbable g = grabbablesInsideTrigger.Find(x => x == pendingGrabbedObject || pendingGrabbedObject.GetComponentInChildren<Grabbable>()) ;
        ////PendingGrabbable is not inside the trigger;
        //if (g == null)
        //{
        //    ReleasePendingGrabbedObject();
        //}
        grabbablesInsideTrigger.Clear();
    }

    private void OnTriggerExit(Collider other)
    {
        if (pendingGrabbedObject == null)
            return;

        ReleasePendingGrabbedObject();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (debugCanvasController.freePlacementMode)
            return;

        Grabbable g = other.GetComponent<Grabbable>();
        if (g == null)
        {
            g = other.GetComponentInParent<Grabbable>();
            if (g == null)
            return;
        }
        if (grabbedObject == null && pendingGrabbedObject == null)
        {
            if (g == lastGrabbedObject && !canGrabLastGrabbedObject)
                return;
            if (g.StartGrabbingAttempt(target))
            {
                g.iTimer.OnFinishInteraction += GrabObject; //Subscribe to the timer end event
                pendingGrabbedObject = g;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (debugCanvasController.freePlacementMode)
            return;

        Grabbable g = other.GetComponent<Grabbable>();
        if (g == null)
        {
            g = other.GetComponentInParent<Grabbable>();
            if (g == null)
                return;            
        }

        grabbablesInsideTrigger.Add(g);

        if (grabbedObject == null)
        {
            if (g == lastGrabbedObject && !canGrabLastGrabbedObject)
                return;
            g.GrabbingUpdate(); //TODO rethink this
        }

    }

    private void ReleasePendingGrabbedObject()
    {
        pendingGrabbedObject.iTimer.OnFinishInteraction -= GrabObject;
        pendingGrabbedObject.StopGrabbingAttempt();
        pendingGrabbedObject = null;
    }

    public void CheckIfCanGrabLastGrabbedObject()
    {
        if (!canGrabLastGrabbedObject)
        {
            if (lastGrabbedObject == null)
                canGrabLastGrabbedObject = true;
            else
            {
                Vector3 projection = Vector3.ProjectOnPlane(this.transform.position - lastGrabbedObject.transform.position, cam.transform.forward);
                canGrabLastGrabbedObject = projection.sqrMagnitude > minimumSquaredDistanceToRegrabPlacedObject;
            }
        }
    }

    public void ClearHand()
    {
        if (grabbedObject != null)
            ReleaseObject();
        else if (pendingGrabbedObject != null)
            ReleasePendingGrabbedObject();
    }

    public void OnDisable()
    {
        ClearHand();
    }

    private void OnDestroy()
    {
        ClearHand();
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 3);
            if (target != null)
                Gizmos.DrawWireSphere(target.position, 0.5f);
        }

    }
}