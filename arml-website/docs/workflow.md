# Building and running applications
The following steps describe in broad terms the workflow of developing an application and deploying it on the ARML device.

### Initialize Unity project:
Follow the instructions in [Installation](./installation.md#installing-the-unity-package) to create a new Unity project and install the ARML SDK Unity package.

### Edit Unity project: 
1. When the ARML welcome window appears, click "Open HelloWorld scene". To recover the ARML window, go to the ARML menu and click "Welcome Window".
2.  Press the play button to start the game in the editor. You can test the game by using keyboard controls to simulate moving (W,A,S,D) and orienting (⬆️⬇️⬅️➡️) the lantern.
3. If you want to edit the scene, we suggest duplicating it and editing the duplicate.

### Connect the ARML to the computer:
Follow the [Configure the USB Ethernet](./os.md#configure-the-usb-ethernet) guide to connect the ARML directly to a desktop or laptop computer over ethernet.

## Alternative method for deploying to the ARML
If you do not want to or cannot connect the ARML directly to your build computer, you can alternatively copy build files from your computer to the ARML.
1. Copy the build directory to a USB drive. 
2. Using a hub, plug a mouse, keyboard, monitor and USB-C power supply (PD) into the USB-C port on the back of the ARML. The ARML will boot and show the launcher app on the monitor.
3. Press the "X" menu item on the Launcher menu to return to the Ubuntu desktop.
4. Insert the USB drive into the hub and copy the build directory to the “unitybuilds” directory on the Ubuntu desktop.
5. Run `unitybuilds/_applauncher/linux_applauncher.x86_64` to restart the launcher.

## Configure player settings
1. Go to `Edit -> Project Settings -> Player` and enter the following configuration:
   - Company Name: FuBILab 
   - Product Name: ARML
2. The above settings can be personalized for your company and project, but must match those set in the build of the [Launcher application](./launcher.md) in order for the settings to be shared across applictions. Therefore, if you change the company or product name in Player Settings, make sure to rebuild and deploy the Launcher:
   - In the "Scene List" for the build, inlcude only the `Assets/ARML/ARMLCore/Scenes/AppLauncher` scene.
   - Build and deploy to the special Launcher directory on the ARML, by default: `/home/fubintlab/Desktop/unitybuilds/_applauncher/linux_applauncher.x86_64`

## Build the application
When ready to build the application for the ARML, follow these configuration steps in the Build Settings window:
1. Go to `File -> Build Profiles` 
2. Select "Linux" under Platforms or add a new build profile for Linux.
3. In the “Scene List", make sure Logic Scene is added and checked, as well as the game scene you want to run. Make sure the Logic Scene is first (index 0) and the game scene is second (index 1).
4. Use the “Build” button to build the application to the shared network directory on the ARML. For example, if you are building a game called "MyFirstGame", the build path would look like `\\192.168.121.1\unitybuilds\MyFirstGame\MyFirstGame.x86_64`. If you are not using the network deployment method, build to a local directory and copy the output files to the ARML as described above in [Alternative method](#alternative-method-for-deploying-to-the-arml).
5. If you are updating an existing application, you can run it again from the launcher to see your changes. If you are adding a new application, you will need to exit the launcher and restart it by running `unitybuilds/_applauncher/linux_applauncher.x86_64`.

## Debug the application
While the application is running on the ARML, press the menu button on the ARML remote control to show the debug screen. Debug information includes:  

- In the lower-left corner, the Unity debug log, which contains the same as the log in the Unity editor. You can show or hide this log from the debug menu.
- In the upper-right corner is the VIO tracking position, as a vector.
- In the upper-left corner is the current frame rate
