using AClockworkBerry;
using Mirror;
using System;
using UnityEngine;

/// <summary>
/// Represents a network player in the ARML application. This class handles different player types, their behaviors, 
/// and interactions in a networked environment.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    GameObject geometryParent;
    AdminUIController adminUI;
    ARMLNetworkManager manager;
    public static event Action OnPlayerLoaded;

    enum PlayerType
    {
        LanternPlayer,
        AdminPlayer
    }

    PlayerType playerType;

    public static NetworkPlayer Instance { get; private set; }

    /// <summary>
    /// Initializes the singleton instance and performs setup operations.
    /// </summary>
    private void Awake()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
        {
            //Destroy(this); //Don't destroy because there is a player for each connected device, I think
        }
        else
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        manager = FindObjectOfType<ARMLNetworkManager>();
        adminUI = FindObjectOfType<AdminUIController>(true);
    }

    /// <summary>
    /// Called when the local player starts. It sets up player type and UI visibility based on the player's role.
    /// </summary>
    public override void OnStartLocalPlayer()
    {
        if (isServer)
        {
            playerType = PlayerType.LanternPlayer;
            if (adminUI != null)
                adminUI.SetCanvasVisibility(false);
        }
        else if (manager.isAdmin)
        {
            playerType = PlayerType.AdminPlayer;
            //adminUI.SetCanvasVisibility(true);
            string message = "Admin device joined server";
            //Debug.Log(message);
            CmdSendScreenLoggerMessage(message);
        }

        OnPlayerLoaded?.Invoke();

        //Subscribe to events only if local player
        PostProcessingController.OnPostProcessingChanged += CmdUpdatePostProcessing;
        ScreenLogger.OnScreenLoggerToggled += CmdToggleScreenLogger;

    }

    /// <summary>
    /// Initial setup for the player, determining the player type.
    /// </summary>
    private void Start()
    {
        //TODO Improve this
        if (!isLocalPlayer)
        {
            if (isServer)
                playerType = PlayerType.AdminPlayer;
            else
            {
                playerType = PlayerType.LanternPlayer;
            }

        }

        name = playerType.ToString();

        //geometryParent = GameObject.Find("--GEOMETRY--");
    }

    /// <summary>
    /// Handles player input updates. Should only be called for local players.
    /// </summary>
    void Update()
    {
        if (!isLocalPlayer) return;

        float xAxisValue = Input.GetAxis("Horizontal");
        float zAxisValue = Input.GetAxis("Vertical");

        //if (xAxisValue > 0.1f || zAxisValue > 0.1f || xAxisValue < -0.1f || zAxisValue < -0.1f)
        //MoveGeometry(xAxisValue, zAxisValue);

        //Transfer objects authority to client
        if (Input.GetKeyDown(KeyCode.T))
        {
            CmdTransferAuthority(connectionToClient);
        }
    }

    //Unused
    void MoveGeometry(float xAxisValue, float zAxisValue)
    {
        if (geometryParent != null)
        {
            //Rotation
            //geometryParent.transform.Rotate(-zAxisValue * speed, xAxisValue * speed, 0);

            //Force Z rotation to 0
            Vector3 currentRotation = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);

            //Translation
            Vector3 movementVector = (geometryParent.transform.forward * zAxisValue * 10) +
                (geometryParent.transform.right * xAxisValue * 10);

            geometryParent.transform.position += new Vector3(movementVector.x, 0, movementVector.z) * Time.deltaTime;

            UpdateObjectPosition(geometryParent, geometryParent.transform.position);
        }
    }

    /// <summary>
    /// Network command to update an object's position.
    /// </summary>
    /// <param name="go">The game object to move.</param>
    /// <param name="newPosition">The new position for the game object.</param>
    [Command]
    void UpdateObjectPosition(GameObject go, Vector3 newPosition)
    {
        go.transform.position = newPosition;
    }

    /// <summary>
    /// Network command to update post processing settings.
    /// </summary>
    /// <param name="config">The new post processing configuration.</param>
    /// <param name="go">The game object to apply the configuration to.</param>
    [Command]
    void CmdUpdatePostProcessing(PostProcessingConfig config, GameObject go)
    {
        go.GetComponent<PostProcessingController>().SetPostProcessingConfig(config);
    }

    /// <summary>
    /// Network command to toggle the screen logger's visibility.
    /// </summary>
    /// <param name="b">Boolean flag to show or hide the screen logger.</param>
    [Command]
    public void CmdToggleScreenLogger(bool b)
    {
        ScreenLogger.Instance.ShowLog = b;
    }

    /// <summary>
    /// Network command to send a message to the screen logger.
    /// </summary>
    /// <param name="message">The message to log.</param>
    [Command]
    void CmdSendScreenLoggerMessage(string message)
    {
        Debug.Log(message);
    }

    /// <summary>
    /// Command that transfers authority of objects so they can be controlled from a client. 
    /// Usually done to place objects from the Editor and have it reflect in the lantern.
    /// </summary>
    [Command]
    private void CmdTransferAuthority(NetworkConnectionToClient conn)
    {
        //Transfer Authority of objects to client
        foreach (EditorFreePlacingObject g in FindObjectsOfType<EditorFreePlacingObject>())
        {
            NetworkIdentity i = g.GetComponent<NetworkIdentity>();

            if (i == null) return;

            //Change Sync Mode to Client to Server
            NetworkTransformReliable t = i.GetComponent<NetworkTransformReliable>();
            if (t != null)
            {
                t.syncDirection = SyncDirection.ClientToServer;
            }

            i.AssignClientAuthority(conn);
            Debug.Log($"Transferred ownership of {i.name} to client {conn.identity.name}.");
        }
    }
}
