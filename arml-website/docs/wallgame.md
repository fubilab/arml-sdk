---
title: "Example 1: WallGame"
sidebar_position: 3
---

# Example 1: WallGame
To help you understand how all the components of the ARML SDK work together, let's look at an example of a simple but complete game. 

The Wall Game showcases simple interaction with the ARML, stencil effects, and the use of the Dialogue and Timeline systems. The player will see a wall comprised of bricks of 2 different colours. The ones with a lighter shade are interactable and can be pushed inward to reveal the environment behind (based on the location around the ancient Barcino city). Once all the bricks have been removed, a roman centurion character climbs up a ladder and starts a conversation with the player, eventually leading up to a question of whether the player will help him fix the wall or not. Regardless of the answer, the player can then interact with the fallen bricks on the floor to have them fly back to their original position.

Open `Scenes > WallGame`.

![](./assets/Pasted%20image%2020240119121346.png)
## Scene Hierarchy
- As a Content scene, it contains parent GameObjects holding both visual and interactive elements.
	- ORIGIN. Determines the initial Transform of the LANTERN when the Scene is loaded.
	- LEVELS. Holds a #LevelController. Controls and manages the Levels in the game, including their activation and progression. Each Level is a child GameObject of this one.
	- LIGHTING. Holds all the Lights in the scene as well as Particle Effects if relevant.
	- CHARACTERS. Holds all the Characters in the scene, in this case the Roman Centurion that can be interacted with as well as 2 farmers with an ox and cart that move in the background.
	- GEOMETRY. Holds all the meshes and visual elements of the scene that don't belong to other categories. Can be further divided into Interactive elements and purely visual environment ones. In the case of this scene. It's divided into "BrickWallParent" which holds all the GameObjects related to the breakable Wall, and "Environment" which is everything behind the Wall (everything that is affected by a stencil effect in order to create a "Portal effect").
	- AUDIO. Holds music and global ambience AudioSources. In this scene it only has a looped ambience of sheep sound effects.
	- CANVAS. Holds world space Canvases related to the scene. For this one it holds a DialogueCanvas in order to display the conversation with the Centurion character.

## Visual Assets
![](./assets/Pasted%20image%2020240119123008.png)
- Visually, the scene consists of a terrain made based on height-map information of Barcelona, with a series of roman-themed assets portraying farms, crops, trees, a road, and a villa in the distance. All of these assets are seen through a Wall made of bricks. Some of these bricks can be interacted with to be pushed outward, in order to progressively reveal the environment behind. 
![](./assets/Pasted%20image%2020240119123352.png)
- Apart from the Wall itself, all the visual environments are placed on a "Stencil" layer that determines that they will only be rendered when seen through a GameObject with a specific material "M_Stencil". This creates an illusion that helps bridge the physical and digital elements of the application.
- Due to licensing we have replaced some paid Unity assets that we used for the environment with some placeholders. Specifically, we used some house and vegetation assets from this pack https://assetstore.unity.com/packages/3d/environments/historic/polygon-ancient-empire-low-poly-3d-art-by-synty-224020, as well as some horses with this pack https://assetstore.unity.com/packages/3d/characters/animals/animal-pack-deluxe-99702. For this release they were replaced by this Roman Villa asset (https://sketchfab.com/3d-models/roman-villa-fcc3241662174fbbb146e6cf658293a9) and a sheep shed (https://sketchfab.com/3d-models/sheep-shed-024cd7a8d35147c8b3e3064685c6bf4b).
![](./assets/placeholder%20environment.png)

## Interactables
![](./assets/Pasted%20image%2020240119123837.png)
- All the interactable bricks in the Wall are CameraPointedObjects. These objects allow to trigger UnityEvents when interacted with. They can be interacted with through the use of trigger colliders attached to the Camera, or by measuring the angle between the Camera and the object to determine if it's currently being pointed at by the Lantern. Once a brick has been interacted with (via Dwell, Button, or Voice command), it calls a function from another component called "RigidBodyInteraction", pushing the object's RigidBody in the opposite direction to the forward Vector of the camera. Essentially pushing the brick towards the Camera and removing them from the wall.
![](./assets/Pasted%20image%2020240119124256.png)
- The game is currently set-up so that it will advance once 28 or more bricks have been removed from the wall. In order to monitor the fallen bricks, we use a component called "CollisionCheck" which allows for triggering of UnityEvents once a specific collision has been triggered a determined number of times. In this case, we are monitoring for "OnCollisionExit" events with GameObjects that are a child of a parent "Interactables" object which holds all of the interactable bricks. Once the condition is met, the "PlayNextLevel" function from the #LevelController is called.
- After the user has a brief conversation with a character, they are asked to help rebuild the wall. The user can then interact with the fallen bricks and they will return to their original position via the #MoveTransformToTarget component.
- We can determine the behaviour of a CameraPointedObject depending on the current Level of the game by using LevelFilterEvents, stating the index of the Level where that behaviour should run when the object is interacted with.
 ![](./assets/Pasted%20image%2020240124142303.png)
## Characters
![](./assets/Pasted%20image%2020240119130528.png)
- Roman Centurion. This character appears through an animation once the bricks have been removed from the wall. It serves to give audiovisual feedback to users after they interact with the wall, and to allow for voice-based Dialogue interaction. The character was designed with Character Creator 3 and Blender.
## Timeline
![](./assets/Pasted%20image%2020240119130931.png)
- The Unity Timeline system allows for the execution of code, animations, sound playback, etc. at specific times through a convenient and intuitive time-based UI. It comes with an API that allows a developer to easily extend its functionality through the creation of custom clips and tracks. For the ARML, we have created some of these custom tracks, most relevant to this scene is the DialogueSystemTrack, which lets characters start their Dialogue at specific times. In this scene, once the bricks are removed, a Timeline is played which includes an animation of the Centurion climbing up a ladder and reacting to the player with some sound effects. Once the animation is over, its Dialogue System is called.
## Dialogue
![[Pasted image 20240119131638.png]]
![](./assets/Pasted%20image%2020240119131638.png)
- The ARML SDK comes with a Dialogue Graph system built with the Unity GraphView API. It was used for this scene in order to create a Dialogue for the Centurion character with the option of offering several reply options to the player, which change the way the dialogue will go. For example, the player can answer whether he will help with rebuilding the wall or not.
