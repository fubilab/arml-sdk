# AR Magic Lantern SDK
**[GitHub](https://github.com/fubilab/arml-sdk) ·
  [Documentation](https://fubilab.github.io/arml-sdk/) ·
  [Project Home](https://emil-xr.eu/lighthouse-projects/upf-ar-magic-lantern/) ·
  [Discord](https://discord.gg/zWZT3yKf4q)**
<hr size="1" />

![](./arml-website/docs/images/arml-render-ob.png)

<hr size="1" />

This repository contains a Software Development Kit (SDK) for creating augmented reality (AR) experiences for the AR Magic Lantern (ARML), an ongoing research project led by the [Full-Body Interaction Lab](https://www.upf.edu/web/fubintlab) at [Universitat Pompeu Fabra](https://www.upf.edu/). 



- [Project Home](https://emil-xr.eu/lighthouse-projects/upf-ar-magic-lantern/)  
Background information about the project and its history.

- [SDK Documentation ](https://fubilab.github.io/arml-sdk/)  
Start here if you want to develop with the SDK.

- [Unity template project](./arml-unity/)  
Start building games and experiences for the ARML in Unity.

- [ARML Hardware](https://fubilab.github.io/arml-sdk/docs/hardware.html)  
Learn how to build or obtain the AR Magic Lantern hardware.

## Under the hood

Not necessary for building experiences with the SDK, but useful if you want to go deeper and contribute to the project or experiment with its internals.

- [Generate Documentation](./arml-website/)  
Learn how to edit and build the SDK documentation website.

- [Arduino Control](./arml-arduino/)  
Learn how to edit, build and deploy the code that runs on the ARML's Arduino module. 

## Visual Positioning System [experimental]

The ARML project is developing a Visual Positioning System (VPS) that allows the device to locate itself within a known, pre-mapped area. The VPS is still in the experimental stage and has not yet been integrated into the SDK.

- [Mapping Utilities](./arml-utils/)  
Scripts for recording video from the ARML cameras, which can then be used to create 3D models and train localization AI models.

- [VPS Training](https://github.com/fubilab/vps-training)  
Scripts and documentation for training the VPS of the ARML.

- [VPS Inference](https://github.com/fubilab/vps-inference)  
C++ implementation of the VPS inference runtime.

<hr size="1">
<a href="https://www.upf.edu/web/fubintlab">
<img src="./arml-website/docs/images/FubIntLab.jpg" height="50" margin="5"/></a>
&nbsp;&nbsp;
<a href="https://emil-xr.eu">
<img src="./arml-website/docs/images/emil-logo.png" height="50"/></a>
&nbsp;&nbsp;
<a href="https://upf.edu">
<img src="./arml-website/docs/images/UPF.png" height="50"/></a>
<hr size="1">
<img src="./arml-website/docs/images/funded-by-the-eu.png" height="50" />

