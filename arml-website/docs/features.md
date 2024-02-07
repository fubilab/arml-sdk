---
title: SDK Features 
sidebar_position: 6
---

## Camera and Rendering

The SDK contains several features that provide rendering configuration and optimization for the ARML device runtime. They are built on the URP (Universal Render Pipeline) for Unity, so the user must choose that rendering pipeline when configuring their Unity project. If the user started their ARML application by copying the template in the SDK (recommended), this rendering pipeline has already been chosen and configured. 
Rendering features and preset customizations include:
-	Render settings that are optimal for the ARML graphics subsystem,
-	Stencil layers and shaders that facilitate portal effects (see Wall Game Walkthrough)
The SDK template project contains a camera component with several customizations, including:
-	Field of view that matches the projection FOV,
-	A vignette canvas object that creates the effect of the flashlight borders in the projection,

Most importantly, the camera also contains the components that transfer the orientation and position data from the sensor and tracking subsystems to the camera’s orientation and position.

## Object Interaction

The SDK includes several components that are placed on Unity game objects to add interaction capabilities such as activating, grabbing and dropping to the object. These components communicate with the camera object to facilitate player interaction with objects in the scene. When configuring the interaction for a specific object, the user can choose between 3 different modes of interaction: (a) Dwell, where the camera must linger on the object for a specified amount of time before the interaction begins, (b) Button, where the player must press a button on the ARML to initiate the interaction and (c) Voice, where the player must speak to interact with the object (see Voice Commands below). For an example of these interactions and more detailed explanation of the various object interaction components and modalities, see the game walkthroughs.

##	Non-playable characters
There are a few utility components in the SDK to help users create engaging non-playable characters (NPCs) in their applications. These components primarily assist with tasks related to the character’s face, including adding a life-like blinking effect and a component that moves the face towards where the player is standing. See the NPC section of the component reference for details.

##	Dialogue system
The SDK implements and extends an open source dialogue system for Unity (Wafflus/unity-dialog-system on GitHub) that uses Unity’s new Graph View API to present a node-based form of designing character dialogue. The system allows the user to create dialogue nodes that defines the audio, text and animation of a piece of the dialogue script. Users create dialogue flow and branching by adding multiple-choice options to a node and connecting them to other nodes. 
Dialogues are attached to objects with which the player will interact, which would normally be non-playable characters (NPCs). The process of creating and editing a dialogue flow for an NPC is covered in the Wall Game walkthrough below. 

##	Voice Commands and Speech-to-text
When configuring object interaction (see above), the SDK user may choose “VOICE” as the interaction type. They then must specify a list of voice command keywords as a parameter of the component. Note that each keyword entry specified in the list must be a single word. The player can then initiate the interaction for the object configured for voice command by holding the button on the ARML for 0.5 seconds and then speaking the command. A tone sounds on the ARML after the button is held to indicate that the player can start speaking.
Another voice interaction feature available in the SDK is Voice Dialogue Control (VDC). The VDC system is integrated into the Dialogue system (see above) so that an existing dialogue flow can be controlled by the player’s voice. The player can interact with the dialogue using their voice by default as long as the “Voice Command Mode” is activated in the SDK configuration (see below). The VDC automatically generates keywords to listen for in a player response from the response options specified in the dialogue node. As with voice commands, the player responds to a dialogue prompt by holding the ARML button for 0.5 seconds and, after the tone, speaking the response.

##	Timeline
The SDK extends the Unity timeline API to provide the user a way of sequencing actions within their game/application. Using an interface familiar from non-linear editing software such as Adobe Premiere Pro, users can add visual blocks to the timeline to control the sequence of gameplay elements such as animation, dialogue, audio cues, game object activation, etc.
