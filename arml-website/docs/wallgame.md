# Example 1: WallGame
To help you understand how all the components of the ARML SDK work together, let's look at an example of a simple but complete game. 

The Wall Game showcases simple interaction with the ARML, stencil effects, and the use of the Dialogue and Timeline systems. The player will see a wall comprised of bricks of 2 different colours. The ones with a lighter shade are interactable and can be pushed inward to reveal the environment behind (based on the location around the ancient Barcino city). Once all the bricks have been removed, a roman centurion character climbs up a ladder and starts a conversation with the player, eventually leading up to a question of whether the player will help him fix the wall or not. Regardless of the answer, the player can then interact with the fallen bricks on the floor to have them fly back to their original position.

Open `Scenes > WallGame`.

![](images/Pasted%20image%2020240119121346.png)  
*Screenshot of WallGame hierarchy, scene view and camera view*

## Scene Hierarchy
As a Content scene, it contains parent GameObjects holding both visual and interactive elements.  

| Node       | Description |
|:-----------|:------------|  
| ORIGIN     | Determines the initial Transform of the LANTERN when the Scene is loaded. |
| LEVELS     | Holds a #LevelController. Controls and manages the Levels in the game, including their activation and progression. Each Level is a child GameObject of this one. |
| LIGHTING   | Holds all the Lights in the scene as well as Particle Effects if relevant. |
| CHARACTERS | Holds all the Characters in the scene, in this case the Roman Centurion that can be interacted with as well as 2 farmers with an ox and cart that move in the background. |
| GEOMETRY   | Holds all the meshes and visual elements of the scene that don't belong to other categories. Can be further divided into Interactive elements and purely visual environment ones. In the case of this scene. It's divided into "BrickWallParent" which holds all the GameObjects related to the breakable Wall, and "Environment" which is everything behind the Wall (everything that is affected by a stencil effect in order to create a "Portal effect"). |
| AUDIO      | Holds music and global ambience AudioSources. In this scene it only has a looped ambience of sheep sound effects. |
| CANVAS     | Holds world space Canvases related to the scene. For this one it holds a DialogueCanvas in order to display the conversation with the Centurion character. |

## Visual Assets

![](images/Pasted%20image%2020240119123008.png)
*Left: top-down view of WallGame scene, showing camera, wall, and landscape*  
*Right: View of the landscape through the wall, showing the use of the stencil layer*

Visually, the scene consists of a terrain made based on height-map information of Barcelona, with a series of roman-themed assets portraying farms, crops, trees, a road, and a villa in the distance. All of these assets are seen through a Wall made of bricks. Some of these bricks can be interacted with to be pushed outward, in order to progressively reveal the environment behind.  

Apart from the Wall itself, all the visual environments are placed on a "Stencil" layer that determines that they will only be rendered when seen through a GameObject with a specific material "M_Stencil". This creates an illusion that helps bridge the physical and digital elements of the application.

### Asset Attribution

The following table lists the source of 3D models used in the SDK (if we did not model them internally). The screenshots are from an internal demonstrator version that contain some commercial assets that could not be included in the SDK. The commercial alternatives are noted and linked in the table below.

| Visual asset       | License | Author | Commercial alternative |
|:---|:---|:---|:---|  
| [Wheat Bag](https://www.cgtrader.com/free-3d-models/military/other/sandbag-1) | Royalty Free No AI | [icekazim](https://www.cgtrader.com/designers/icekazim) |
| [Sheep shed](https://sketchfab.com/3d-models/sheep-shed-024cd7a8d35147c8b3e3064685c6bf4b) | CC Attribution | [Maria Stashko](https://sketchfab.com/maria_stashko) | [POLYGON Ancient Empire](https://assetstore.unity.com/packages/3d/environments/historic/polygon-ancient-empire-low-poly-3d-art-by-synty-224020) |
| [Animated Sheep](https://sketchfab.com/3d-models/sheep-test-non-commercial-196bb78e6e6343888d09f468a6a9dbc7) | CC Attribution-NonCommercial | [Nyilonelycompany](https://sketchfab.com/Nyilonelycompany) | [Animal pack deluxe](https://assetstore.unity.com/packages/3d/characters/animals/animal-pack-deluxe-99702) |
| [Wooden Crate](https://sketchfab.com/3d-models/ikea-wooden-crate-4c5d81d4b18644df9f9f2959f198f186) | CC Attribution | [szymon.burek](https://sketchfab.com/szymon.burek) 
| [Roman Villa](https://sketchfab.com/3d-models/roman-villa-fcc3241662174fbbb146e6cf658293a9) | CC Attribution | [deltorvik](https://sketchfab.com/deltorvik) | [POLYGON Ancient Empire](https://assetstore.unity.com/packages/3d/environments/historic/polygon-ancient-empire-low-poly-3d-art-by-synty-224020) |
| [Roman Centurion Armor](https://sketchfab.com/3d-models/roman-centurion-armor-d0c6de99f16c49f386a9f8d7c3120dec) | CC Attribution | [Tactical_Beard](https://sketchfab.com/Tactical_Beard) |


## Interactables
![](images/Pasted%20image%2020240119123837.png)
![](images/Pasted%20image%2020240119124256.png)
*Screenshots from Unity editor showing parameters of interactable objects in the WallGame example scene.*

All the interactable bricks in the Wall are CameraPointedObjects. These objects allow to trigger UnityEvents when interacted with. They can be interacted with through the use of trigger colliders attached to the Camera, or by measuring the angle between the Camera and the object to determine if it's currently being pointed at by the Lantern. Once a brick has been interacted with (via Dwell, Button, or Voice command), it calls a function from another component called "RigidBodyInteraction", pushing the object's RigidBody in the opposite direction to the forward Vector of the camera. Essentially pushing the brick towards the Camera and removing them from the wall.

The game is currently set-up so that it will advance once 28 or more bricks have been removed from the wall. In order to monitor the fallen bricks, we use a component called "CollisionCheck" which allows for triggering of UnityEvents once a specific collision has been triggered a determined number of times. In this case, we are monitoring for "OnCollisionExit" events with GameObjects that are a child of a parent "Interactables" object which holds all of the interactable bricks. Once the condition is met, the "PlayNextLevel" function from the #LevelController is called.

After the user has a brief conversation with a character, they are asked to help rebuild the wall. The user can then interact with the fallen bricks and they will return to their original position via the #MoveTransformToTarget component.

We can determine the behaviour of a CameraPointedObject depending on the current Level of the game by using LevelFilterEvents, stating the index of the Level where that behaviour should run when the object is interacted with.

 ![](images/Pasted%20image%2020240124142303.png)  
 *Screenshot from Unity editor showing the use of Level Filter Events to trigger different actions from an interactable depending on the current level of the game.*
## Characters
![](images/Pasted%20image%2020240119130528.png)  
*Screenshot from Unity editor showing the Roman Centurion character.*

**Roman Centurion** This character appears through an animation once the bricks have been removed from the wall. It serves to give audiovisual feedback to users after they interact with the wall, and to allow for voice-based Dialogue interaction. The character was designed with Character Creator 3 and Blender.

## Timeline
![](images/Pasted%20image%2020240119130931.png)
*Screenshot from Unity editor showing extended timeline editor.*

The Unity timeline system allows for the execution of code, animations, sound playback, etc. at specific times through a convenient and intuitive time-based UI. It comes with an API that allows a developer to easily extend its functionality through the creation of custom clips and tracks. For the ARML, we have created some of these custom tracks, most relevant to this scene is the DialogueSystemTrack, which lets characters start their dialogue at specific times. In this scene, once the bricks are removed, a timeline is played which includes an animation of the Centurion climbing up a ladder and reacting to the player with some sound effects. Once the animation is over, its dialogue system is called.

## Dialogue
![](images/arml-wallgame-dialogue.png)
*Screenshot from Unity editor showing dialogue system editor for Wall Game example.*

The ARML SDK comes with a Dialogue Graph system built with the Unity GraphView API. It was used for this scene in order to create a Dialogue for the Centurion character with the option of offering several reply options to the player, which change the way the dialogue will go. For example, the player can answer whether he will help with rebuilding the wall or not.
