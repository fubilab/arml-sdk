using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosPose = RosMessageTypes.MagicLantern.PosRotMsg;


public class ROSTracking : MonoBehaviour
{
    [SerializeField] bool trackPosition;
    [SerializeField] bool trackRotation;

    private Vector3 rosStartEuler = Vector3.zero;
    private Vector3 camStartEuler;
    private Vector3 rosStartPos = Vector3.zero;
    private Vector3 camStartPos;
    private Vector3 rosPos;
    private Quaternion rosRot;

    Vector3 EulerRot(Quaternion qRot)
    {
        Vector3 rot = qRot.eulerAngles;
        return new Vector3(-rot.x, rot.y, rot.z);
    }

    void Start()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        return;
#endif
        NetworkPlayer.OnPlayerLoaded += StartProcess;
    }

    void StartProcess()
    {
        camStartEuler = EulerRot(transform.rotation);
        camStartPos = transform.localPosition;
        ROSConnection.GetOrCreateInstance().Subscribe<RosPose>("pose", PoseChange);
    }

    void PoseChange(RosPose poseMessage)
    {
        // if (!RosErrorFlagReader.noError)
        // {
        //     return;
        // }

        rosPos = new Vector3(poseMessage.pos_y, poseMessage.pos_z, -poseMessage.pos_x);
        rosRot = new Quaternion(poseMessage.rot_z, -poseMessage.rot_y, -poseMessage.rot_x, poseMessage.rot_w);

        //Do first frame
        if (rosStartEuler == Vector3.zero)
        {
            rosStartEuler = EulerRot(rosRot);
            rosStartPos = rosPos;
            Debug.Log(rosStartEuler);
            return;
        }

        //Rotation
        Vector3 eulerDiff = EulerRot(rosRot) - rosStartEuler;
        if (trackRotation)
        {
            Vector3 remappedEulerDiff = new Vector3(eulerDiff.z, -eulerDiff.y, eulerDiff.x);
            transform.rotation = Quaternion.Euler(camStartEuler + remappedEulerDiff);
        }

        //Position
        Vector3 correctedRosPos = rosPos - rosStartPos;
        if (trackPosition)
            transform.localPosition = correctedRosPos + camStartPos;
    }

    private void Update()
    {
        //Reset Position
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!trackPosition) return;
            print("Position Reset");
            rosStartPos = rosPos;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (!trackRotation) return;
            print("Rotation Reset");
            rosStartEuler = EulerRot(rosRot);

        }
    }
}