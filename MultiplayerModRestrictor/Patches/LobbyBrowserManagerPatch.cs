using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Steamworks;
using Unity.Netcode;

namespace MultiplayerModRestrictor.Patches;

[HarmonyPatch(typeof(LobbyBrowserManager))]
public static class LobbyBrowserManagerPatch {
    [HarmonyPatch(nameof(LobbyBrowserManager.OnLobbyCreated))] [HarmonyPostfix]
    public static void AddLobbyData(LobbyBrowserManager __instance)
    {
        SteamMatchmaking.SetLobbyData(new CSteamID(__instance.currentSteamLobbyID), "mmrestrictor", MultiplayerModRestrictor.GetVersionText());
    }
}