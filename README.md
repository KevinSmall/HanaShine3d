# HanaShine3d

A Unity client for the SAP HANA SHINE demo content.

For more information:
http://scn.sap.com/community/hana-in-memory/blog/2014/05/31/a-3d-game-frontend-for-the-sap-hana-shine-demo-content

To help orientate yourself in the project:

## SCENES
The main scenes are in the _Scenes folder:
* gamestart - GUI screen for initial login
* level01 - ignore this scene, it was for testing
* level02 - ignore this scene, it was for testing
* terrainScene - the main game scene. it should be runnable directly, without going via the gamestart scene, and defaults to playing offline. This instant execution is handled by the game object in the scene called "_dummy to allow instant scene Execution", which executes the "GameLogin" script with the "run immediately" flag set.

## SCRIPTS
All scripts are in the _Scripts folder.

## DOCUMENTATION
See further documentation on the code, see [the /Docs folder here](https://github.com/KevinSmall/HanaShine3d/blob/master/Docs/Unity%20Client%20Architecture.md).

## PREFABS
Most prefabs are in the Prefabs folder.