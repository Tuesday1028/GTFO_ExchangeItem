using Hikaria.ExchangeItem.Handlers;
using Hikaria.ExchangeItem.Managers;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Settings;
using TheArchive.Core.Localization;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.ExchangeItem.Features;

[EnableFeatureByDefault]
[DisallowInGameToggle]
public class ExchangeItem : Feature
{
    public override string Name => "资源交换";

    public override bool InlineSettingsIntoParentMenu => true;

    public static new ILocalizationService Localization { get; set; }

    [FeatureConfig]
    public static ExchangeItemSetting Settings { get; set; }

    public class ExchangeItemSetting
    {
        [FSDisplayName("物品交换按键")]
        public KeyCode ExchangeItemKey { get; set; } = KeyCode.T;
    }

    public override void OnFeatureSettingChanged(FeatureSetting setting)
    {
        _updater?.OnInteractionKeyChanged();
    }

    private static ExchangeItemUpdater _updater;

    [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
    private class LocalPlayerAgent__Setup__Patch
    {
        private static void Postfix(LocalPlayerAgent __instance)
        {
            _updater = __instance.GetComponent<ExchangeItemUpdater>() ?? __instance.gameObject.AddComponent<ExchangeItemUpdater>();
        }
    }

    [ArchivePatch(typeof(PlayerInventoryLocal), nameof(PlayerInventoryLocal.DoWieldItem))]
    private class PlayerInventoryLocal__DoWieldItem__Patch
    {
        private static void Postfix(PlayerInventoryLocal __instance)
        {
            if (!__instance.AllowedToWieldItem)
                return;

            _updater?.OnWieldItemChanged();
        }
    }

    [ArchivePatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.SetStorageData))]
    private class PlayerAmmoStorage__SetStorageData__Patch
    {
        private static void Postfix(PlayerAmmoStorage __instance)
        {
            if (_updater != null)
            {
                var playerAgent = __instance.m_playerBackpack?.Owner?.PlayerAgent?.TryCast<PlayerAgent>();
                if (playerAgent != null)
                    _updater.OnAmmoStorageChanged(playerAgent);
            }
        }
    }

    public override void Init()
    {
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ExchangeItemUpdater>();

        ExchangeItemManager.Setup();
    }
}
