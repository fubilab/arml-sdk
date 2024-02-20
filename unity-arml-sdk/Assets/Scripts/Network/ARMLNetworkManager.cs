using ARML.GameBuilder;
using Mirror;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using UnityEngine;

public class ARMLNetworkManager : NetworkManager
{
    [Header("ARML")]
    public bool isAdmin = false;
    public bool autoConnectToHotspot = false;

    public static new ARMLNetworkManager singleton { get; private set; }

    private void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(this.gameObject);
            return;
        }

        singleton = this;
        base.Awake();
    }

    void Start()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        //isAdmin = true; // Uncomment when network is used again
#elif !UNITY_EDITOR && UNITY_STANDALONE_LINUX
        isAdmin = false; // Auto Host when running from Linux Lantern
#endif

        if (!isAdmin)
        {
            StartHost();
            GetComponent<NetworkManagerHUD>().enabled = false;
            // TODO: Handle on-screen notifications for Admin connected/disconnected etc.
        }
        else
        {
            if (autoConnectToHotspot)
                networkAddress = GetDefaultGateway()?.ToString();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Host has started server");
    }

    public override void OnStopServer()
    {
        StartCoroutine(UnloadScenes());
    }

    public override void OnStopClient()
    {
        if (mode == NetworkManagerMode.Offline)
            StartCoroutine(UnloadScenes());
    }

    public override void OnServerConnect()
    {
        base.OnServerConnect();
        Debug.Log("Client has connected to server");
    }

    IEnumerator UnloadScenes()
    {
        Debug.Log("Unloading Subscenes");
        yield return Resources.UnloadUnusedAssets();
    }

    public void SetNetworkAddress(string networkAddress)
    {
        this.networkAddress = networkAddress ?? string.Empty;
    }

    public static IPAddress GetDefaultGateway()
    {
        IPAddress iPAddress = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Where(n => n.Name.StartsWith("ARML"))
            .SelectMany(n => n.GetIPProperties()?.GatewayAddresses)
            .Select(g => g?.Address)
            .Where(a => a != null)
            .FirstOrDefault();

        if (iPAddress != null)
            return iPAddress;
        else
        {
            Debug.LogError("ARML Hotspot not found! Check your connection settings");
            return null;
        }
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        StartCoroutine(SceneController.Instance.FadeBackToGame(GameController.Instance.GetCurrentGameSceneName()));
    }
}