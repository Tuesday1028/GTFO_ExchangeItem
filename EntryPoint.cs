using System.Collections.Generic;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.Localization;

[assembly: ModDefaultFeatureGroupName("Exchange Item")]

namespace Hikaria.ExchangeItem;

[ArchiveDependency(Core.PluginInfo.GUID, ArchiveDependency.DependencyFlags.HardDependency)]
[ArchiveModule(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : IArchiveModule
{
    public void Init()
    {
        Instance = this;

        Logs.LogMessage("OK");
    }

    public void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
    }

    public void OnLateUpdate()
    {
    }

    public void OnExit()
    {
    }

    public static EntryPoint Instance { get; private set; }

    public bool ApplyHarmonyPatches => false;
    public bool UsesLegacyPatches => false;

    public ArchiveLegacyPatcher Patcher { get; set; }

    public string ModuleGroup => "Exchange Item";

    public Dictionary<Language, string> ModuleGroupLanguages => new()
    {
        { Language.English, "Exchange Item" }, { Language.Chinese, "资源交换" }
    };
}
