# Logic Scene
![](../../assets/Pasted%20image%2020240119114241.png)

![](../../assets/Pasted%20image%2020240119113131.png)
## Scene Hierarchy
- A persistent scene that is loaded throughout the whole application runtime. Works as both a boot-strap and a logic scene containing several Controller scripts and a GameObject representing the Lantern that contains all the camera and UI functionality. It contains 2 Prefabs called "--CONTROLLERS--" and "--LANTERN--" which can be found in "Assets/Prefabs" and dragged and drop into any scene to convert it into a Logic Scene.
## Controllers
- A collection of Controller scripts that handle logic related to Scene Management, global interaction settings, post-processing, voice detection etc.
	- SceneController: Manages scene transitions and operations, including loading scenes and applying fade effects.
	- GameController: Manages the overall game state, including scene loading and optional score handling.
	- InteractionTypeController: Allows for global changes at runtime of InteractionType (Dwell, Button, Voice) for objects in the game.
	- STTSystem: GameObject holding logic for the functioning of the voice detection system.
	- PostProcesingController: Manages the post-processing effects in the scene, allowing dynamic changes to visual effects.
## Lantern
- Parent GameObject holding all the functionality related to tracking, camera, and screen-space lantern UI (vignette, crosshair, debug indicators, etc.). 