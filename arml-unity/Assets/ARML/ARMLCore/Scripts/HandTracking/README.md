# Hand tracking

Tracks both hands (21 joints each) with the OAK-D camera, next to the Spectacular AI VIO. The hand models (MediaPipe
palm detection and hand landmarks) run on the OAK-D itself, inside the same DepthAI pipeline as the VIO, so there is no
second process and no conflict over the camera.

## Use it in a scene

1. `Vio` component: enable **Enable Hand Tracking** (or `"useHandTracking": true` in `launcherSettings.json` when
   *Read Launcher Settings* is on).
2. Add a **HandTracker** component and assign the camera's `PoseProvider`. Read the hands from `HandTracker.Left` and
   `HandTracker.Right`: `Tracked`, `Joints` (Unity world space, indexed by `HandJoint`), `Gesture`, `PalmCenter`,
   `PinchDistance`.
3. Optional: **HandSkeletonVisualizer** draws the joints, **HandTrackingDebugOverlay** shows rates and latency (F1).

`Assets/ARML/Examples/HandTracking/Scenes/HandTrackingExample.unity` is the example scene (VIO, PoseProvider, hand tracking, skeleton, overlay). The overlay shows the OAK camera image as the background of the whole view, behind the 3D hands (F2 toggles it, F1 hides the text), to line the hands up with the real image and the projection. `Camera View Fit` chooses between filling the view (crops), fitting the whole image, or stretching. The overlay also prints the field of view of the OAK next to the one of the Unity camera: set the vertical field of view of the Unity camera to the OAK one for the 3D skeleton to line up with the image.
It also runs in the Unity Editor (Play) when the OAK-D is connected and not used by another program (`Run In Editor`
on the `Vio` component, on by default). The Editor keeps the native plugin loaded, so `spectacularAI_unity.dll` can
only be replaced after closing the Editor. If the camera cannot be opened, the `Vio` component logs the reason instead
of throwing. `ARMLCore.Editor.HandTrackingPlayTest.Run` runs the scene in Play mode in batch mode (see the file).

## Models

The OpenVINO blobs live in `Assets/StreamingAssets/HandTracking` (see `NOTICE.txt` and the licence files there).
`palm_detection_sh4.blob` and `hand_landmark_full_sh4.blob` are versioned (exceptions in `arml-unity/.gitignore`,
plain git like the Vosk models). `hand_landmark_lite_sh4.blob` is faster but less accurate and stays local, because
`*.blob` is otherwise git-ignored: copy it from `depthai_hand_tracker/models` and set `Landmark Model File` on the
`Vio` component to use it. The same files work for every OAK-D and for Windows and Linux builds.

## Things to know

* **Handedness.** The model classifies hands as it sees them. Depending on how the camera is mounted left and right
  can be swapped: use `HandTracker.swapHandedness`.
* **Hand distance.** Without depth the size of an average hand is assumed, so the distance is only accurate to about
  10%. Stereo depth fixes the size of the user's hand, but the right camera of ARML devices has an IR-pass lens, so
  it only works with the OAK IR dot projector (`Hand Tracking Use Depth` and `Hand Tracking Ir Projector Intensity`
  on `Vio`). The projector adds IR dots to the left camera image, which may affect the VIO: check before enabling.
* **Latency.** The hand tracking runs at about 16-22 Hz with 100-160 ms from the camera exposure to the result. The
  hands are placed with the camera pose at the capture time and moved to the current camera pose, so camera motion
  during that time is compensated, but hand motion is not.
* **Device load and heat.** The networks run on a device that is also busy with the VIO. `FastVio` makes each network
  call noticeably faster. The OAK-D gets hot: in free air on a desk the VIO alone settles at about 78 C and with hand
  tracking the chip reached 100 C within four minutes, after which the networks stopped answering. Hand tracking
  therefore searches for new hands at a modest rate and slows down when the camera is hot. `HandTracker.DeviceTemperature`,
  `ThrottleLevel` and `Stalled` report this (also shown in the debug overlay). Check the temperature in the ARML
  enclosure before relying on it.
* **Color frame rate.** `Hand Tracking Color Fps` (15) lowers the frame rate of the color camera: it runs cooler and the
  networks answer faster, and hand tracking does not run faster than that anyway.
* **Linux.** The native plugin (`spectacularAI_unity`) must be rebuilt for Linux, see
  `sai-unity/plugin/README.md`. The old `.so` has no hand tracking: the `Vio` component logs an error and disables it.
* **Command line (example scene).** `-handDepth 0|1 -handIr 0..1 -handModel file.blob -handMaxHands 1|2 -handSwap 0|1
  -handSmooth 0|1 -handLog file.csv`.
