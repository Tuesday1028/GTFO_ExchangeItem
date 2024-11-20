using Gear;
using Hikaria.ExchangeItem.Handlers;
using Hikaria.ExchangeItem.Managers;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.ExchangeItem.Features;

[EnableFeatureByDefault]
[DisallowInGameToggle]
public class ExchangeItem : Feature
{
    public override string Name => "Exchange Item";

    public override bool InlineSettingsIntoParentMenu => true;

    public static new ILocalizationService Localization { get; set; }

    [FeatureConfig]
    public static ExchangeItemSetting Settings { get; set; }

    public class ExchangeItemSetting
    {
        [FSDisplayName("物品交换按键")]
        public KeyCode ExchangeItemKey { get => ExchangeItemHandler.ExchangeItemKey; set => ExchangeItemHandler.ExchangeItemKey = value; }
    }

    public static string Prompt_Exchange => Localization.Get(1);
    public static string Prompt_TargetToSource => Localization.Get(2);
    public static string Prompt_SourceToTarget => Localization.Get(3);

    [ArchivePatch(typeof(PlayerInteraction), nameof(PlayerInteraction.UpdateWorldInteractions))]
    private class PlayerInteraction__UpdateWorldInteractions__Patch
    {
        private static bool Prefix()
        {
            return !ExchangeItemManager.InteractionAllowed;
        }
    }

    [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
    private class LocalPlayerAgent__Setup__Patch
    {
        private static void Postfix(LocalPlayerAgent __instance)
        {
            ExchangeItemManager.LocalPlayer = __instance;
            GameObject go = __instance.gameObject;
            if (go.GetComponent<ExchangeItemHandler>() == null)
            {
                go.AddComponent<ExchangeItemHandler>();
            }
            if (go.GetComponent<ExchangeItemUpdater>() == null)
            {
                go.AddComponent<ExchangeItemUpdater>();
            }
        }
    }

    public override void Init()
    {
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ExchangeItemHandler>();
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ExchangeItemUpdater>();

        ExchangeItemManager.Setup();
    }
}
