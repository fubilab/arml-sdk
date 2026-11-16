using System;
using SpectacularAI.DepthAI;
using UnityEngine;

namespace ARML.HandTracking
{
    /// <summary>
    /// Command line overrides for testing builds of the hand tracking example:
    ///   -handDepth 0|1   -handIr 0..1   -handModel file.blob   -handMaxHands 1|2   -handSwap 0|1   -handSmooth 0|1
    ///   -handLog path.csv
    /// Runs before the Vio component starts its session.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class HandTrackingCommandLine : MonoBehaviour
    {
        public Vio vio;
        public HandTracker tracker;
        public HandTrackingDebugOverlay overlay;

        private void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                string value = args[i + 1];
                switch (args[i])
                {
                    case "-handDepth":
                        if (vio != null) vio.HandTrackingUseDepth = value != "0";
                        break;
                    case "-handIr":
                        if (vio != null) vio.HandTrackingIrProjectorIntensity = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    case "-handModel":
                        if (vio != null) vio.LandmarkModelFile = value;
                        break;
                    case "-handMaxHands":
                        if (vio != null) vio.HandTrackingMaxHands = int.Parse(value);
                        break;
                    case "-handSwap":
                        if (tracker != null) tracker.swapHandedness = value != "0";
                        break;
                    case "-handSmooth":
                        if (tracker != null) tracker.smooth = value != "0";
                        break;
                    case "-handLog":
                        if (overlay != null) overlay.csvPath = value;
                        break;
                }
            }
        }
    }
}
