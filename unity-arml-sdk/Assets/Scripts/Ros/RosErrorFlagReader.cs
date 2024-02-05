using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using ErrorStatus = RosMessageTypes.MagicLantern.ErrorStatusMsg;

public class RosErrorFlagReader : MonoBehaviour
{
    public string errorFlagTopic = "error_status_topic";
    public static bool noError;

    private void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<ErrorStatus>(errorFlagTopic, ErrorFlagCallback);

    }

    private void ErrorFlagCallback(ErrorStatus message)
    {
            
        if (message.no_error)   
        {
            noError = true;
        }
        else
        {
            noError = false;
        }
    }   
}