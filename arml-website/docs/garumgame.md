---
title: "Example 2: GarumGame"
sidebar_position: 4
---

# Example 2: GarumGame

The Garum Game is designed to showcase a wider range of interactions with the ARML. It mostly focuses on the use of Grabbables and PlacementTargets, along with CollisionChecks. When the game starts, the player is introduced to a Garum Cook character that welcomes the player and offers information about the Garum recipe that is going to be done. First he encourages the player to grab a knife and cut the fish. Then to grab the Spoon, scoop salt from a bowl, and pour it over the bowl where the sauce is being cooked. After that, to grab some bay leaves. Finally, the player is instructed to grab a stirring stick and move it within the cooking sauce for a bit. 

Open `Scenes > GarumGame`.

![](./assets/Pasted%20image%2020240123163632.png)  
*Screenshot from Unity editor showing the elements of the GarumGame example.*

## Scene Hierarchy
Similar to the Wall Game Scene, this is a Content Scene so it contains geometry, interactables, and logic. We will skip the elements that serve the same purpose as in the previous scene.
- **CHARACTERS:** There is a Garum Cook character that offers explanations to prepare the Garum sauce. Says different things based on the way the player is interacting with the scene.
- **GEOMETRY:** During development it holds a 3D scan of the physical space where the activity will be performer, in order to help with the placement of virtual objects etc.
- **INTERACTABLES:** This Scene has a lot of Interactables so it was decided to place them on their own parent GameObject. Further explanation on following section.
- **CANVAS:** Holds a World-Space canvas that shows the current progression of the different Tasks in the Game.

## Visual Assets
This scene has a focus on the interactables elements and not so much on the environment visuals.A series of tools and ingredients were modeled and animated in order to give visual feedback to the interactions. For instance, the model of a mackerel was sectioned using the Bisect tool in Blender, as it will be cut later using the Knife tool. A Particle System was created to simulate falling salt when interacting with the Spoon.

## Interactables
### Knife and Fish
The Knife is an AnchoredGrabbable that attaches to the camera. It uses the "OnButtonDownWhileGrabbed" Event of the Grabbable in order to activate an Animation Trigger that plays a downward cutting motion Animation. This Animation also activates a Trigger Collider in the edge of the Knife. This specific Collider is the one that the sectioned mackerel fish will react to through its CollisionCheck. When this CollisionCheck passes, each of the fish parts will move towards the location of the garum sauce in the dolium via its MoveTransformToTarget component.

![](./assets/Pasted%20image%2020240124155606.png)  
*Screenshots of knife interacting with fish*

### Spoon and Salt
The Spoon is an AnchoredGrabbable. The OnButtonDownWhileGrabbed activates an Animation Trigger that plays a turning motion animation. If this animation is played while in direct contact with the Salt Bowl, another animation will play that will "fill up" (increase the scale) of a salt mound on the spoon. When the Spoon plays the animation while not in contact with the salt bowl, it plays a ParticleAnimation simulating the falling of grains of salt, which also reduces the scale of the mound until it becomes 0, and the "salt" runs out. The goal is to fill the spoon with salt and to activate the falling ParticleSystem so that the salt particles fall on the Garum Dolium. 

![](./assets/Pasted%20image%2020240124155712.png)  
*Screenshots of spoon interacting with salt*

### Tool Placement Targets

These are two PlacementTarget objects (shown in blue in the image), one each for the Knife and the spoon, so that they can be placed and picked up again. Those 2 actions can be performed via dwell, button, or voice command interactions.

![](./assets/Pasted%20image%2020240124155757.png)  
*Screenshot of tool placement targets*

### Herbs
Some of the leaves of this mesh are separated from the rest and have a CameraPointedObject controller. When interacted with they will move towards the garum dolium via MoveTransformToTarger.

![](./assets/Pasted%20image%2020240124160558.png)  
*Screenshot of herbs*

### Garum Dolium and Stirring Stick
Once the other ingredients have been interacted with, the last step is to stir the sauce. This is done through a vertically-placed stirring stick. It can be grabbed as an AnchoredGrabbable and moved around within the dolium. The trick here is that the sauce receptacle has a CollisionCheck that is checking for a specifically-named collider (the Stirring Stick) in its OnCollisionStay call with a velocity over a certain threshold. While the stirring stick is moving within the garum dolium collider, it counts as a succesful check.

![](./assets/Pasted%20image%2020240124160940.png)  
*Screenshot of garum dolium, stirring stick, tool placement targets and herbs*