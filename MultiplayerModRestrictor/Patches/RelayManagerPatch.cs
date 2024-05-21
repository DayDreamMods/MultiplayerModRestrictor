using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using CessilCellsCeaChells.CeaChore;
using HarmonyLib;
using Newtonsoft.Json;
using Unity.Netcode;

[assembly: RequiresField(typeof(ConnectionPayload), nameof(ConnectionPayload.MMRestrictorVersions), typeof(string), true)]

namespace MultiplayerModRestrictor.Patches;

[HarmonyPatch(typeof(RelayManager))]
public static class RelayManagerPatch {
    [HarmonyPatch(nameof(RelayManager.ApprovalCallback))] [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> InsertNewResponseCase(IEnumerable<CodeInstruction> codeInstructions)
    {
        var codeMatcher =  new CodeMatcher(codeInstructions);
        codeMatcher.MatchForward(true, 
                new CodeMatch(instruction => instruction.opcode == OpCodes.Ldarg_2),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Ldc_I4_0),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Stfld),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Ldarg_2),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Ldflda),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Initobj))
            .ThrowIfInvalid("Failure to find patch location in RelayManager::ApprovalCallback!");
        codeMatcher.Insert(
            new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Call, typeof(RelayManagerPatch).GetMethod(nameof(CheckModVersions))));
            
        return codeMatcher.Instructions();
    }

    [HarmonyPatch(nameof(RelayManager.GetPayloadBytes))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> InsertNewFieldAssignment(IEnumerable<CodeInstruction> codeInstructions)
    {
        var codeMatcher =  new CodeMatcher(codeInstructions);
        codeMatcher.MatchForward(true, 
                new CodeMatch(instruction => instruction.opcode == OpCodes.Newobj),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Dup),
                new CodeMatch(instruction => instruction.opcode == OpCodes.Ldsfld))
            .ThrowIfInvalid("Failure to find patch location in RelayManager::GetPayloadBytes!");
        codeMatcher.Insert(
            new CodeInstruction(OpCodes.Call, typeof(MultiplayerModRestrictor).GetMethod(nameof(MultiplayerModRestrictor.GetVersionText))),
            new CodeInstruction(OpCodes.Stfld, typeof(ConnectionPayload).GetField(nameof(ConnectionPayload.MMRestrictorVersions))),
            new CodeInstruction(OpCodes.Dup));
            
        return codeMatcher.Instructions();
    }

    public static void CheckModVersions(ConnectionPayload? connectionPayload, NetworkManager.ConnectionApprovalResponse response)
    {
        if (connectionPayload == null)
        {
            MultiplayerModRestrictor.Logger?.LogWarning("ConnectionPayload was null!");
            return;
        }
        
        if (!MultiplayerModRestrictor.CompareModDatas(connectionPayload.MMRestrictorVersions, out var err))
        {
            response.Reason = err;
            response.Approved = false;
        }
    }
}