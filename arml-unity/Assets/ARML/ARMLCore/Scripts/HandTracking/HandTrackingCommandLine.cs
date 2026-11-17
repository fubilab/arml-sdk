using System;
using SpectacularAI.DepthAI;
using UnityEngine;

namespace ARML.HandTracking
{
    /// <summary>
    /// Command line overrides for testing builds of the hand tracking example:
    ///   -handDepth 0|1   -handIr 0..1   -handModel file.blob   -handMaxHands 1|2   -handSwap 0|1   -handSmooth 0|1
    ///   -handLog path.csv
    /// and for tests with the projector, where nobody may have a keyboard at hand:
    ///   -handFeed 0|1                     camera image behind the scene (the camera sees what is projected)
    ///   -handVisual off|dots|skeleton     what is drawn on the hands, dots is the least intrusive
    ///   -handBackground default|black|gray|white|red|green|blue
    ///   -handLabels 0|1                   gesture label above each hand
    ///   -handText 0|1                     the text of the overlay
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
            HandSkeletonVisualizer visualizer = FindFirstObjectByType<HandSkeletonVisualizer>();
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
                    case "-handFeed":
                        if (overlay != null) overlay.showCameraView = value != "0";
                        break;
                    case "-handVisual":
                        if (visualizer != null && Enum.TryParse(value, true, out HandVisualMode mode)) visualizer.mode = mode;
                        break;
                    case "-handBackground":
                        if (overlay != null)
                        {
                            if (value.Equals("default", StringComparison.OrdinalIgnoreCase)) overlay.background = ProjectionBackground.SceneDefault;
                            else if (Enum.TryParse(value, true, out ProjectionBackground background)) overlay.background = background;
                        }
                        break;
                    case "-handLabels":
                        if (overlay != null) overlay.showHandLabels = value != "0";
                        break;
                    case "-handText":
                        if (overlay != null) overlay.visible = value != "0";
                        break;
                }
            }
        }
    }
}
