using HarmonyLib;
using Steamworks;

namespace MultiplayerModRestrictor.Patches;

[HarmonyPatch(typeof(LobbyBrowserLobbyPanel))]
public static class LobbyBrowserLobbyPanelPatch {
    [HarmonyPatch(nameof(LobbyBrowserLobbyPanel.OnPointerUp))] [HarmonyPrefix]
    public static bool PreventJoining(LobbyBrowserLobbyPanel __instance)
    {
        var steamMatchmakingData = SteamMatchmaking.GetLobbyData(__instance.steamLobbyID, "mmrestrictor") ?? "";
        MultiplayerModRestrictor.Logger?.LogDebug(steamMatchmakingData);
        if (MultiplayerModRestrictor.CompareModDatas(steamMatchmakingData, out var err)) return true;
        MultiplayerModRestrictor.Logger?.LogDebug($"Skipping lobby: {__instance.serverNameText.text} because {err}");
        MainMenuManager.Singleton.ShowUIGroup_FailedToJoinServer();
        MainMenuManager.Singleton.reasonForServerDisconnectText.text = err;
        return false;
    }
}