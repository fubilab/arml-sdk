using FishNet.Example;
using UnityEngine;

namespace ARML.Network
{
    public class DisableNetworkCanvas : MonoBehaviour
    {
        [SerializeField] NetworkHudCanvases networkHudCanvas;

        // Start is called before the first frame update
        void Start()
        {
            networkHudCanvas.GetComponent<Canvas>().enabled = false;
        }
    }
}