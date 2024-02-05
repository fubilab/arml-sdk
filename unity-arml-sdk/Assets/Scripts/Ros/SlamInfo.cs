using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RtabInfo = RosMessageTypes.MagicLantern.RtabmapInfoMsg;
using RtabOdom = RosMessageTypes.MagicLantern.RtabmapOdomMsg;
using TrackingStatus = RosMessageTypes.MagicLantern.TrackingStatusMsg;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;


public class SlamInfo : MonoBehaviour
{
    public GameObject cube;

    void Start()
    {

        ROSConnection.GetOrCreateInstance().Subscribe<RtabInfo>("rtabmap_info", InfoDisplay);
        ROSConnection.GetOrCreateInstance().Subscribe<RtabOdom>("rtabmap_odom_info", OdomDisplay);
        ROSConnection.GetOrCreateInstance().Subscribe<TrackingStatus>("tracking_status", TrackingDisplay);

    }

    void InfoDisplay(RtabInfo infoMessage)
    {
        if (!RosErrorFlagReader.noError)
        {
            return;
        }
        // Publish the rtabmap useful information
        Debug.Log("Header: " + infoMessage.header);
        Debug.Log("Id: " + infoMessage.refId);
        Debug.Log("Loop Closure Id: " + infoMessage.loopClosureId);
        Debug.Log("Proximity Detection Id: " + infoMessage.proximityDetectionId);
        Debug.Log("Landmark Id: " + infoMessage.landmarkId);
    }

    void OdomDisplay(RtabOdom odomMessage)
    {
        if (!RosErrorFlagReader.noError)
        {
            return;
        }
        // Publish the odom useful 
        Debug.Log("Header: " + odomMessage.header);
        Debug.Log("odom Lost: " + odomMessage.lost);
        Debug.Log("Number of matches: " + odomMessage.matches);
        Debug.Log("Number of inliers: " + odomMessage.inliers);
        Debug.Log("Number of features: " + odomMessage.features);
    }

    void TrackingDisplay(TrackingStatus TrackingMessage)
    {
        if (!RosErrorFlagReader.noError)
        {
            return;
        }
        // Publish the odom useful 
        Debug.Log("World Tracking Status: " + TrackingMessage.worldTracking);
        Debug.Log("Anchor Tracking Status: " + TrackingMessage.anchorTracking);

    }

}