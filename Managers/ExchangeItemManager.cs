using Hikaria.Core;
using Hikaria.Core.SNetworkExt;
using Player;
using SNetwork;
using System.Collections.Generic;
using System.Linq;
using Version = Hikaria.Core.Version;

namespace Hikaria.ExchangeItem.Managers;

public class ExchangeItemManager
{
	private static readonly Version MinVersion = new Version(2, 0, 0);

	public static void Setup()
	{
		CoreAPI.OnPlayerModsSynced += OnPlayerModsSynced;
		GameEventAPI.OnMasterChanged += OnMasterChanged;
		s_ExchangeItemRequestPacket = SNetExt_Packet<pExchangeItemRequest>.Create(typeof(pExchangeItemRequest).FullName, MasterDoExchangeItem, DoExchangeItemValidate);
		s_ExchangeItemFixPacket = SNetExt_Packet<pExchangeItemFix>.Create(typeof(pExchangeItemFix).FullName, ReceiveExchangeItemFix, null, true, SNet_ChannelType.GameOrderCritical);
    }

    private static void OnPlayerModsSynced(SNet_Player player, IEnumerable<pModInfo> mods)
    {
        if (player.IsMaster)
        {
            MasterHasExchangeItem = mods.Any(m => m.GUID == PluginInfo.GUID && m.Version >= MinVersion);
        }
    }

    private static void OnMasterChanged()
    {
        MasterHasExchangeItem = CoreAPI.IsPlayerInstalledMod(SNet.Master, PluginInfo.GUID, MinVersion);
    }

	private static void ReceiveExchangeItemFix(ulong sender, pExchangeItemFix data)
	{
		var localPlayerAgent = PlayerManager.GetLocalPlayerAgent();
        if (localPlayerAgent == null)
            return;

        localPlayerAgent.Sync.WantsToSetFlashlightEnabled(LastFlashLightStatus, true);
        localPlayerAgent.Inventory.ReceiveSetFlashlightStatus(LastFlashLightStatus, false);
        GuiManager.PlayerLayer.Inventory.UpdateAllSlots(SNet.LocalPlayer, localPlayerAgent.Inventory.WieldedSlot);
    }

    private static void DoExchangeItemValidate(pExchangeItemRequest data)
	{
        if (SNet.IsMaster)
        {
			MasterDoExchangeItem(SNet.LocalPlayer.Lookup, data);
        }
    }


    private static void MasterDoExchangeItem(ulong sender, pExchangeItemRequest data)
	{
        if (!SNet.IsMaster || !data.Source.TryGetPlayer(out var source) || !data.Target.TryGetPlayer(out var target))
			return;

        while (true)
		{
            var inventorySlot = data.Slot;
            AmmoType ammoType = (inventorySlot == InventorySlot.ResourcePack) ? AmmoType.ResourcePackRel : AmmoType.CurrentConsumable;
            PlayerBackpackManager.TryGetBackpack(source, out var playerBackpack);
            PlayerBackpackManager.TryGetBackpack(target, out var playerBackpack2);
            bool flag = playerBackpack.TryGetBackpackItem(inventorySlot, out var backpackItem);
            bool flag2 = playerBackpack2.TryGetBackpackItem(inventorySlot, out var backpackItem2);
            if (flag2 && flag)
            {
                pItemData_Custom pItemData_Custom = new pItemData_Custom
                {
                    ammo = playerBackpack.AmmoStorage.GetAmmoInPack(ammoType),
                    byteId = backpackItem.Instance.pItemData.custom.byteId,
                    byteState = backpackItem.Instance.pItemData.custom.byteState
                };
                backpackItem.Instance.SetCustomData(pItemData_Custom, false);
                pItemData_Custom pItemData_Custom2 = new pItemData_Custom
                {
                    ammo = playerBackpack2.AmmoStorage.GetAmmoInPack(ammoType),
                    byteId = backpackItem2.Instance.pItemData.custom.byteId,
                    byteState = backpackItem2.Instance.pItemData.custom.byteState
                };
                backpackItem2.Instance.SetCustomData(pItemData_Custom2, false);
                PlayerBackpackManager.MasterRemoveItem(backpackItem.Instance, source);
                PlayerBackpackManager.MasterRemoveItem(backpackItem2.Instance, target);
                PlayerBackpackManager.MasterAddItem(backpackItem.Instance, target);
                PlayerBackpackManager.MasterAddItem(backpackItem2.Instance, source);
                break;
            }
            if (!flag2 && flag)
            {
                pItemData_Custom pItemData_Custom3 = new pItemData_Custom
                {
                    ammo = playerBackpack.AmmoStorage.GetAmmoInPack(ammoType),
                    byteId = backpackItem.Instance.pItemData.custom.byteId,
                    byteState = backpackItem.Instance.pItemData.custom.byteState
                };
                backpackItem.Instance.SetCustomData(pItemData_Custom3, false);
                PlayerBackpackManager.MasterRemoveItem(backpackItem.Instance, source);
                PlayerBackpackManager.MasterAddItem(backpackItem.Instance, target);
                break;
            }
            if (flag2 && !flag)
            {
                pItemData_Custom pItemData_Custom4 = new pItemData_Custom
                {
                    ammo = playerBackpack2.AmmoStorage.GetAmmoInPack(ammoType),
                    byteId = backpackItem2.Instance.pItemData.custom.byteId,
                    byteState = backpackItem2.Instance.pItemData.custom.byteState
                };
                backpackItem2.Instance.SetCustomData(pItemData_Custom4, false);
                PlayerBackpackManager.MasterRemoveItem(backpackItem2.Instance, target);
                PlayerBackpackManager.MasterAddItem(backpackItem2.Instance, source);
				break;
            }
			return;
        }

        if (!source.IsBot)
		    s_ExchangeItemFixPacket.Send(default, source);
        if (!target.IsBot)
            s_ExchangeItemFixPacket.Send(default, target);
    }

	public static float ReceiverAmmoInPack { get; private set; }
	public static float LocalAmmoInPack { get; private set; }

    public static void WantToExchangeItem(SNet_Player target, InventorySlot slot)
	{
		var localPlayerAgent = PlayerManager.GetLocalPlayerAgent();
		if (localPlayerAgent == null)
			return;

        LastFlashLightStatus = localPlayerAgent.Inventory.WantsFlashlightEnabled;
        s_ExchangeItemRequestPacket.Ask(new(SNet.LocalPlayer, target, slot));
    }


	public static bool MasterHasExchangeItem { get; private set; }

    private static bool LastFlashLightStatus;

    private static SNetExt_Packet<pExchangeItemRequest> s_ExchangeItemRequestPacket;
    private static SNetExt_Packet<pExchangeItemFix> s_ExchangeItemFixPacket;
}

public struct pExchangeItemFix
{
}

public struct pExchangeItemRequest
{
	public pExchangeItemRequest(SNet_Player source, SNet_Player target, InventorySlot slot)
	{
		Source.SetPlayer(source);
		Target.SetPlayer(target);
		Slot = slot;
	}

	public SNetStructs.pPlayer Source = new();

	public SNetStructs.pPlayer Target = new();

	public InventorySlot Slot = InventorySlot.None;
}
