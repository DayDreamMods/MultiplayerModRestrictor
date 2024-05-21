## MultiplayerModRestrictor
A Last Train Outta' Wormtown plugin that facilitates other plugins restricting multiplayer access by mod versioning.

Plugins wishing to implement this should depend on it in their manifest.json `www_Day_Dream-MultiplayerModRestrictor-0.4.1` and `[BepInDependency("wwwDayDream.MultiplayerModRestrictor")]` attribute.

Create an internal class attribute of the name 'MMReqVersion' or 'MMReqExist' and apply it to your `BaseUnityPlugin`.

* MMReqVersion: Requires versions to match in order for clients to be in the same lobby.
* MMReqExist: Requires the mod to be present on both clients but without version checking.