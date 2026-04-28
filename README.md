# Mechanic Simulator

Unity project restored and verified with `Unity 6000.3.10f1`.

## Open the project

1. Open the folder in Unity Hub.
2. Use Unity `6000.3.10f1`.
3. Let Unity resolve packages from `Packages/manifest.json`.

## Main scene

The main project scene included in the provided files is:

- `Assets/Project/Scenes/Actions/Chassis/FrontSuspensionAction.unity`

## Notes

- The project now compiles successfully in Unity batch mode.
- Some provided UI prefabs reference scene names such as `GarageEnvironment` and `MainMenuAction`, but those scene files were not present in the delivered project files. The loader now handles that case safely by logging a warning instead of breaking compilation.
