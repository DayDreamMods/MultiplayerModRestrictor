using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;

namespace MultiplayerModRestrictor;

[AttributeUsage(AttributeTargets.Class)]
internal class MMReqVersion : Attribute {}
[AttributeUsage(AttributeTargets.Class)]
internal class MMReqExist : Attribute {}

[Serializable]
public class ModData {
    public string ModGUID;
    public string ModName;
    public string ModVersion;
}

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
}