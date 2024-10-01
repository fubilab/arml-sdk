using FishNet.Example;
using UnityEngine;

namespace ARML
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