---
title: Script Reference 
sidebar_position: 8
---

The following summarizes the function of the important scripts in the ARML Unity SDK.

## 📁 Character  
### 📄 CharacterBlink  
Component to place on a rigged character to make them blink at random intervals.
### 📄 GenericIKLook, HumanoidIKLook  
Component that lets you choose a rigged character and a target. Keeps the head of the character pointed at the target.

## 📁 Audio  
### 📄 STTMicController  
Component to run at the scene level that controls when the audio input is listening or not. 
Depends on: VoskSpeechToText, DialogSystem

## 📁 Debug
### 📄 CameraParentController  
Lets the user control the camera with the keyboard for debugging
### 📄 DebugCanvasController  
Toggles visibility of debug panels when button is pressed on the remote
### 📄 ExportPositionData
Records position data over time and exports it as JSON when a key is pressed. Also can load a saved JSON file and playback the position data over time.

## 📁 Dialogue System 
Extended the type of Component references in the Nodes. Implemented the run time component to make calls to Character AudioSources, text UI etc.

The Dialogue System uses the following project as a starting point: https://github.com/Wafflus/unity-dialogue-system/tree/master 

The original project offers a framework and UI for designing dialogues and storing text information.

The ARML SDK extends the type of component references that the Nodes can have by adding the option to trigger audio files and animations. It also supports the execution of an arbitrary number of different UnityEvents when specific Nodes are reached. A run-time component was also developed that is responsible for executing the calls to the different audio and animation components, as well as handling the current state of a character’s dialogue and the interation with it (either via pointing at reply options or through voice interaction).

## 📁 IconManager
Collection of scripts (downloaded) that add label gizmos automatically to Interactables.

## 📁 Interaction
### 📄 ActionFeedback
Component that triggers sound effects or particles when the object is interacted with.
### 📄 CameraPointedObject 
Component which triggers interaction events when the camera is pointing directly at the object.
### 📄 CollisionCheck 
Component which triggers interaction events when a collision behavior happens. Possible collision behaviors: onCollision, onEnter, onExit.
### 📄 Interactable
Abstract class. Base component for any item that user can interact with. Configures type of interaction and provides interaction utilities like timers.
### 📄 InteractionTypeController 
Allows interaction type to be changed at runtime and for other objects to listen for that change.
### 📄 RigidBodyInteraction 
Added to objects that should be manipulated with physics.
### 📄 Grabbing
Grabbable Abstract class. An interactable object which can be grabbed. Triggers event when object is grabbed and also manages state between grabbed and not-grabbed.
### 📄 AnchoredGrabbable 
Implementation of Grabbable that once grabbed attaches itself to an anchor
### 📄 CameraGrabber 
Component which triggers a grab action by listening to the interaction timer event
### 📄 Placement Target 
Component that allows Grabbables to be placed and picked up from a defined position.

## 📁 Scene Management
### 📄 ApplicationLauncher 
Main logic script for the App Launcher (see SceneReference).
### 📄 Level
A grouping of game objectives represented as Tasks. It controls the sequence of events that happen during the game, and the current state and goals of the game stage. If it's on an object that also has a Timeline component, it's responsible for the playback of the timeline, in order to control time-sensitive events.
### 📄 LevelController
Controller class responsible for holding references to all the Levels and handling their execution.
### 📄 SceneController
Provides functions for loading and transitioning between different Scenes, and resetting the state of a scene.
### 📄 Task
State-holding class that describes an activity to be added to a Level. Defines the activity name, tracks the progress and completion.

## 📁 Timeline 
Collection of scripts that extend the Unity Timeline functionality. See [[Timeline Reference]].

## 📁 Tracking
Collection of scripts that interface with the device sensors for tracking orientation.

## 📁 User Interface
### 📄 TaskCrosshairController
Responsible for determining the contextual icon displayed in the Lantern Crosshair.
### 📄 TaskTaskProgress
UI responsible for displaying the current progress of the current Level's Tasks.

## 📁 Utilities
Collection of Utility scripts to perform common functions with Unity objects such as modifying their transform.
### 📄 Vosk
https://github.com/alphacep/vosk-unity-asr


