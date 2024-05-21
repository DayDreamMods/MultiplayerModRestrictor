using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;

namespace MultiplayerModRestrictor;

[AttributeUsage(AttributeTargets.Class)]
internal class MMReqVersion : Attribute {}
[AttributeUsage(AttributeTargets.Class)]
internal class MMReqExist : Attribute {}

[MMReqVersion] [MMReqExist]
[BepInAutoPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public partial class MultiplayerModRestrictor : BaseUnityPlugin {
    
    
    public new static ManualLogSource? Logger { get; private set; }
    public static List<ModData> ModDatas = [ ];
    public static Harmony Patcher { get; } = new(MyPluginInfo.PLUGIN_GUID);

    public void Awake()
    {
        Logger = base.Logger;

        Patcher.PatchAll();

        int patchedMethodCount;
        Logger.LogDebug($"Successfully patched {(patchedMethodCount = Patcher.GetPatchedMethods().Count())} method{(patchedMethodCount == 1 ? "" : "s")}.");
    }

    public void Start() => GatherModVersions();
    private void OnDestroy() => GatherModVersions();

    private void GatherModVersions()
    {
        if (ModDatas.Count > 0) return;
        Logger?.LogDebug("Gathering Mod Restrictions...");
        
        foreach (var (pluginGuid, pluginInfo) in BepInEx.Bootstrap.Chainloader.PluginInfos)
        {
            var requiresVersionSync = pluginInfo.Instance.GetType().CustomAttributes.Any(attr => attr.AttributeType.Name == "MMReqVersion");
            var requiresExistenceSync = requiresVersionSync || pluginInfo.Instance.GetType().CustomAttributes.Any(attr => attr.AttributeType.Name == "MMReqExist");

            if (!requiresExistenceSync) continue;
            
            var pluginName = pluginInfo.Metadata.Name;
            var pluginVersion = requiresVersionSync ? pluginInfo.Metadata.Version.ToString() : "*";
            Logger?.LogDebug($"Found mod that requires multiplayer restriction: [{pluginGuid}] {pluginName}");
            
            ModDatas.Add(new ModData(){ ModGUID = pluginGuid, ModName = pluginName, ModVersion = pluginVersion });
        }
        
        Logger?.LogDebug($"Located {ModDatas.Count} mod{(ModDatas.Count == 1 ? "" : "s")}: {GetVersionText()}");
        
    }
    
    public static string GetVersionText()
    {
        var jsonData = JsonConvert.SerializeObject(ModDatas);
        return jsonData;
    }

    public static bool CompareModDatas(string modDataJson, out string error)
    {        
        var modDatas = !string.IsNullOrEmpty(modDataJson) ? 
            JsonConvert.DeserializeObject<ModData[]>(modDataJson) ?? [] : [];

        var canJoin = true;
        var reasonSB = new StringBuilder("Requires:\n");
        foreach (var modData in ModDatas)
        {
            var potentialClientMatch = modDatas.FirstOrDefault(data => modData.ModGUID == data.ModGUID);
            var clientMatchesVersion = potentialClientMatch?.ModVersion == modData.ModVersion;
            
            if (potentialClientMatch != null && clientMatchesVersion) continue;
            
            canJoin = false;
            reasonSB.AppendLine(potentialClientMatch != null
                ? $"Version Mismatch: {modData.ModName} v{modData.ModVersion} (v{potentialClientMatch.ModVersion})"
                : $"Missing Mod: {modData.ModName} v{modData.ModVersion}");
        }

        error = reasonSB.ToString().TrimEnd('\n', '\r');
        return canJoin;
    }
}