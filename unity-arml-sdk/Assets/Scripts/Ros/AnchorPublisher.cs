using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.MagicLantern;

public class AnchorPublisher : SingletonBehavior<AnchorPublisher>
{
    void Start()
    {
        _ros = ROSConnection.GetOrCreateInstance();
        
        _ros.RegisterPublisher<AnchorInformationMsg>(TopicName);

        AnchorDict = new Dictionary<string, AnchorDefinition>();
        DirtyAnchorDefinitions = new Queue<AnchorDefinition>();

        GameObject[] anchorObjects = GameObject.FindGameObjectsWithTag("anchor");
        foreach (GameObject anchorObject in anchorObjects)
        {
            AnchorDefinition anchorDefinition = anchorObject.GetComponent<AnchorDefinition>();
            AnchorDict.Add(anchorDefinition.AnchorId, anchorDefinition);
            DirtyAnchorDefinitions.Enqueue(anchorDefinition);
        }
    }

    void FixedUpdate()
    {
        if (!RosErrorFlagReader.noError)
        {
            return;
        }
        UpdateAnchors();
    }

    private void UpdateAnchors()
    {
        float frequency = _publishMessageFrequency;
        _timeElapsed += Time.deltaTime;

        if (_timeElapsed < frequency)
        {
            return;
        }

        AnchorDefinition anchorDefinition;
        if (!DirtyAnchorDefinitions.TryDequeue(out anchorDefinition))
        {
            return;
        }
        Vector3 anchorPosition = anchorDefinition.transform.position;
        Vector3 rosPos = new Vector3(
            anchorPosition.z,
            -anchorPosition.x,
            anchorPosition.y
        );

        // Create AnchorInfo object and populate the data
        AnchorInformationMsg anchorInfo = new AnchorInformationMsg();
        anchorInfo.id = int.Parse(anchorDefinition.AnchorId);
        anchorInfo.location[0] = anchorPosition.z;
        anchorInfo.location[1] = -anchorPosition.x;
        anchorInfo.location[2] = anchorPosition.y;
        anchorInfo.location[3] = 0;  //  the orientation is not correct, it is necessary to change it to ROS coordinate frame (right hand)
        anchorInfo.location[4] = 0;
        anchorInfo.location[5] = 0;
        anchorInfo.volume_size = 0; // not used atm
        anchorInfo.filter_proximity = anchorDefinition.FilterProximity;

        // Finally send the message to server_endpoint.py running in ROS
        _ros.Publish(TopicName, anchorInfo);

        DebugAnchorInfo(anchorInfo);
        _timeElapsed = 0;
    }

    private void DebugAnchorInfo(AnchorInformationMsg anchorInfo)
    {
        // Publish the anchor information
        Debug.Log("Anchor ID: " + anchorInfo.id);
        Debug.Log("Location:s " + string.Join(", ", anchorInfo.location));
        Debug.Log("Volume Size: " + anchorInfo.volume_size);
        Debug.Log("Filter Proximity: " + anchorInfo.filter_proximity);
    }

    private ROSConnection _ros;
    private float _publishMessageFrequency = 1f;
    // Used to determine how much time has elapsed since the last message was published
    private float _timeElapsed;

    public Dictionary<string, AnchorDefinition> AnchorDict;
    public Queue<AnchorDefinition> DirtyAnchorDefinitions;
    public string TopicName = "anchor_info";
}

