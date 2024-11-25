# Launcher application

The launcher application loads when the ARML boots. It scans the ARML applications directory and displays available applications in a menu. The application directory is usually at `/home/fubintlab/Desktop/unitybuilds`, but the launcher always uses its own path to derive the directory.

The launcher application also has a configuration menu, accessible by pressing the gear icon. The configuration choices here are saved to a JSON file in a shared data directory that is also accessible to the other Unity applications running on the ARML (in Unity, it is the `Application.persistentDataPath`). Applications built with the ARML SDK will read their configuration from this JSON file, so it is a way to persist system settings across all ARML applications.

Startup sound effect by [Justin Callaghan](https://pixabay.com/users/justincallaghan-11325622) from [Pixabay](https://pixabay.com/sound-effects)