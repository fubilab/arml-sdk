using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using PointCloud2 = RosMessageTypes.MagicLantern.PointCloud2Msg;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class FeaturesSubscriber : MonoBehaviour
{
    void Start()
    {

        ROSConnection.GetOrCreateInstance().Subscribe<PointCloud2>(Topic, OnReceivePointCloud);
        _positions = new List<Vector3>();

        // Create an initial set of particles from the point cloud
        // GenerateParticlesFromPointCloud();
    }

    void OnReceivePointCloud(PointCloud2 pointCloud)
    {
        if (!RosErrorFlagReader.noError)
        {
            return;
        }
        _pointCloudData = pointCloud.data;
        _isPointCloudInitialized = true;
        ConvertPointCloudToPositions();
        if (updated != null)
            updated();
    }

    void ConvertPointCloudToPositions()
    {
        // Convert the point cloud data to a list of Vector3 positions
        if (!_isPointCloudInitialized || _pointCloudData == null)
        {
            return;
        }

        // Assuming each point consists of x, y, and z coordinates (each float takes 4 bytes)
        int pointSize = 12;  // Size of each point (3 floats)
        int pointCount = _pointCloudData.Length / pointSize;

        _positions.Clear();

        for (int i = 0; i < pointCount; i++)
        {
            int startIndex = i * pointSize;

            // Extract x, y, and z values from the byte array
            float x = BitConverter.ToSingle(_pointCloudData, startIndex);
            float y = BitConverter.ToSingle(_pointCloudData, startIndex + 4);
            float z = BitConverter.ToSingle(_pointCloudData, startIndex + 8);

            // Create a Vector3 position from the extracted values
            Vector3 position = new Vector3(-y, z, x);

            // Add the position to the list
            _positions.Add(position);
        }
    }

    // ROS variables
    public string Topic = "feature_point_cloud";
    private bool _isPointCloudInitialized = false;
    private byte[] _pointCloudData;

    /// <summary>
    /// Invoked whenever the point cloud is updated.
    /// </summary>
    public event Action updated;

    /// <summary>
    /// An array of positions for each point in the point cloud.
    /// This array is parallel to <see cref="identifiers"/> and
    /// <see cref="confidenceValues"/>. Positions are provided in
    /// point cloud space, that is, relative to this <see cref="ARPointCloud"/>'s
    /// local position and rotation.
    /// </summary>
    public List<Vector3> positions
    {
        get => _positions;
    }

    private List<Vector3> _positions;
}