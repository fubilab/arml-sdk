---
title: Building and Running
sidebar_position: 5
---

# Building and running applications
The following steps describe in broad terms the workflow of developing an application and deploying it on the ARML device.

### 1. Edit Unity project
The SDK includes a Unity project that contains all the components and libraries needed to build and
run applications for the ARML. Use this project as a template by duplicating it and using the copy as the starting point for your application.

### 2. Build the application
In the “Unity Build Settings,” choose platform “Windows, Mac, Linux” and choose Linux as the “Target Platform.” Use the “Build” button to build the application to a local directory. Choose a name for the “Save as” value that clearly identifies your application, because this is the name that will show in the ARML launcher.

### 3. Copy application files to ARML device
1. Copy the build directory to a USB drive. 
2. Open the battery compartment of the ARML and remove the battery so the USB hub is accessible. 
3. Plug a mouse, keyboard and the USB drive into the hub inside the ARML.
4. Turn on the ARML and once the launcher app is visible, press “ESC” key on keyboard to return to the Ubuntu desktop.
5. Copy the build directory from the USB drive into the “unitybuilds” directory on the Ubuntu desktop.
6. In the build directory that you just copied, find the executable file which ends in “.x86_64”, right click and choose “Properties” from the context menu. Navigate to the “Permissions” tab and check the box for “Allow executing file as program.”
7. Close the Properties window.
8. Double click the executable file to run the application.

### 4. Debug the application
While the application is running on the ARML, press the “Q” on the keyboard or the menu button (3 horizontal lines) on the ARML remote control to show debug information in the projection. Debug information includes:  

- In the lower-left corner, the Unity debug log, which contains the same as the log in the Unity editor.
- In the upper-left corner, information about the connection to ROS, which manages the VIO tracking subsystem.
- In the upper-right corner, the display of the debug components (see component guide).
