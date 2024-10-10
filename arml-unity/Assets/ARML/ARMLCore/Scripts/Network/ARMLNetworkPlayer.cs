using AClockworkBerry;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

namespace ARML.Network
{
    /// <summary>
    /// Represents a network player in the ARML application. This class handles different player types, their behaviors, 
    /// and interactions in a networked environment.
    /// </summary>
    public class ARMLNetworkPlayer : NetworkBehaviour
    {
        [SerializeField] SceneField logicScene;

        GameObject geometryParent;
        AdminUIController remoteUI;
        public static event Action OnPlayerLoaded;

        enum PlayerType
        {
            LanternPlayer,
            RemotePlayer
        }

        PlayerType playerType;
        private readonly SyncVar<bool> isHost = new SyncVar<bool>();

        public static ARMLNetworkPlayer Instance { get; private set; }

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
            }

            //manager = FindObjectOfType<ARMLNetworkManager>();
            remoteUI = FindObjectOfType<AdminUIController>(true);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

#if !UNITY_EDITOR
        FindAnyObjectByType<NetworkHudCanvases>().enabled = false;
#endif

            isHost.Value = base.Owner.IsHost; //Need to use SyncVar so non-host client also gets this information about the host

            if (isHost.Value)
            {
                playerType = PlayerType.LanternPlayer;

                if (base.IsOwner)
                {
                    if (remoteUI != null)
                        remoteUI.SetCanvasVisibility(false);
                }
            }
            else
            {
                playerType = PlayerType.RemotePlayer;
                //adminUI.SetCanvasVisibility(true);
            }

            name = playerType.ToString();

            Debug.Log($"Client {base.Owner.ClientId} joined server");

            if (!base.IsOwner) return;

            //SceneManager.AddConnectionToScene(this.LocalConnection, 
            //    UnityEngine.SceneManagement.SceneManager.GetSceneByName(logicScene.SceneName));

            OnPlayerLoaded?.Invoke();

            //Subscribe to events only if local player
            PostProcessingController.OnPostProcessingChanged += CmdUpdatePostProcessing;
            ScreenLogger.OnScreenLoggerToggled += CmdToggleScreenLogger;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            Debug.Log($"Client {base.Owner.ClientId} exit server");
        }

        /// <summary>
        /// Handles player input updates. Should only be called for local players.
        /// </summary>
        void Update()
        {
            if (!IsOwner) return;

            float xAxisValue = Input.GetAxis("Horizontal");
            float zAxisValue = Input.GetAxis("Vertical");

            //if (xAxisValue > 0.1f || zAxisValue > 0.1f || xAxisValue < -0.1f || zAxisValue < -0.1f)
            //MoveGeometry(xAxisValue, zAxisValue);

            //Transfer objects authority to client
            if (Input.GetKeyDown(KeyCode.T))
            {
                foreach (NetworkObject nob in FindObjectsOfType<NetworkObject>())
                {
                    if (nob.GetComponent<ARMLNetworkPlayer>() == null)
                        RequestOwnership(LocalConnection, nob);
                }
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
        [ServerRpc]
        void UpdateObjectPosition(GameObject go, Vector3 newPosition)
        {
            go.transform.position = newPosition;
        }

        /// <summary>
        /// Network command to update post processing settings.
        /// </summary>
        /// <param name="config">The new post processing configuration.</param>
        /// <param name="go">The game object to apply the configuration to.</param>
        [ServerRpc]
        void CmdUpdatePostProcessing(PostProcessingConfig config, GameObject go)
        {
            go.GetComponent<PostProcessingController>().SetPostProcessingConfig(config);
        }

        /// <summary>
        /// Network command to toggle the screen logger's visibility.
        /// </summary>
        /// <param name="b">Boolean flag to show or hide the screen logger.</param>
        [ServerRpc]
        public void CmdToggleScreenLogger(bool b)
        {
            ScreenLogger.Instance.ShowLog = b;
        }

        /// <summary>
        /// Network command to send a message to the screen logger.
        /// </summary>
        /// <param name="message">The message to log.</param>
        [ServerRpc]
        void CmdSendScreenLoggerMessage(string message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// Command that transfers authority of objects so they can be controlled from a client. 
        /// Usually done to place objects from the Editor and have it reflect in the lantern.
        /// </summary>
        [ServerRpc]
        private void RequestOwnership(NetworkConnection conn, NetworkObject nob)
        {
            nob.RemoveOwnership();
            nob.GiveOwnership(conn);

            Debug.Log($"Transferred ownership of {nob.name} to client {conn.ClientId}.");
        }
    }
}