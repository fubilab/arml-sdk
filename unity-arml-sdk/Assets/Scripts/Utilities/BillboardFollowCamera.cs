using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the GameObject this script is attached to always face the main camera.
/// This is commonly used for creating billboarding effects where objects such as
/// sprites or UI elements always face towards the player's view.
/// </summary>
public class BillboardFollowCamera : MonoBehaviour
{
    Camera mainCamera;

    /// <summary>
    /// Initializes and finds the main camera in the scene.
    /// </summary>
    void Start()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Adjusts the GameObject's rotation in the LateUpdate phase to ensure it faces the camera.
    /// This is done after all camera movement for the frame has been processed.
    /// </summary>
    void LateUpdate()
    {
        Vector3 newRotation = mainCamera.transform.eulerAngles;
        newRotation.x = 0;
        newRotation.z = 0;
        transform.eulerAngles = newRotation;
    }
}
