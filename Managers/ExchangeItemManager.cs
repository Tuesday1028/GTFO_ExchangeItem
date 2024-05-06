using Hikaria.Core;
using Hikaria.Core.SNetworkExt;
using Player;
using SNetwork;
using System;
using static Hikaria.ExchangeItem.Features.ExchangeItem;

namespace Hikaria.ExchangeItem.Managers;

public class ExchangeItemManager
{
	public static void Setup()
	{
		GameEventAPI.OnMasterChanged += OnMasterChanged;
		s_ExchangeItemRequestPacket = SNetExt_Packet<pExchangeItemRequest>.Create(typeof(pExchangeItemRequest).FullName, DoExchangeItem, DoExchangeItemValidate);
		s_ExchangeItemFixPacket = SNetExt_Packet<pExchangeItemFix>.Create(typeof(pExchangeItemFix).FullName, ReceiveExchangeItemFix, null, true, SNet_ChannelType.GameOrderCritical);
    }

	private static void OnMasterChanged()
    {
		MasterHasExchangeItem = CoreAPI.IsPlayerInstalledMod(SNet.Master, PluginInfo.GUID);
    }

	private static void ReceiveExchangeItemFix(ulong sender, pExchangeItemFix data)
	{
        GuiManager.PlayerLayer.Inventory.UpdateAllSlots(SNet.LocalPlayer, InventorySlot.None);
    }

    private static void DoExchangeItemValidate(pExchangeItemRequest data)
	{
        if (SNet.IsMaster)
        {
			DoExchangeItem(SNet.LocalPlayer.Lookup, data);
        }
    }


    private static void DoExchangeItem(ulong sender, pExchangeItemRequest data)
	{
        if (!SNet.IsMaster || !data.Source.TryGetPlayer(out var source) || !data.Target.TryGetPlayer(out var target))
			return;

		var sourceAgent = source.PlayerAgent.Cast<PlayerAgent>();
		var targetAgent = target.PlayerAgent.Cast<PlayerAgent>();

		var sourceFlashLightStatus = sourceAgent.Inventory.WantsFlashlightEnabled;
		var targetFlashLightStatus = targetAgent.Inventory.WantsFlashlightEnabled;
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
		sourceAgent.Sync.WantsToSetFlashlightEnabled(sourceFlashLightStatus);
		targetAgent.Sync.WantsToSetFlashlightEnabled(targetFlashLightStatus);
		s_ExchangeItemFixPacket.Send(default, source, target);
    }

    public static bool InteractionAllowed { get; private set; }

	public static event Action OnUpdated;

	public static void Update()
	{
		SetInventoryItem();
		InteractionAllowed = IsInteractionAllowed;
		var onUpdated = OnUpdated;
		if (OnUpdated != null)
			onUpdated();
	}

	private static bool IsInteractionAllowed => MasterHasExchangeItem 
		&& TargetPlayerAgent != null && exchangeType != ExchangeType.Invalid
		&& LocalPlayerWieldingSlot != InventorySlot.ResourcePack
		&& LocalPlayer.Locomotion.m_currentStateEnum != PlayerLocomotion.PLOC_State.Downed;

	public static void DoClear()
	{
		InteractionAllowed = false;
		LocalPlayerWieldingItem = null;
		LocalPlayerWieldingSlot = InventorySlot.None;
		TargetWieldItem = null;
		TargetWieldSlot = InventorySlot.None;
		LocalItem = null;
		LocalPlayerAmmoInPack = 0f;
		TargetItem = null;
		TargetAmmoInPack = 0f;
		exchangeType = ExchangeType.Invalid;
	}

	public static PlayerAgent TargetPlayerAgent { get; private set; }

	public static ItemEquippable LocalPlayerWieldingItem { get; private set; }

	public static InventorySlot LocalPlayerWieldingSlot { get; private set; }

	public static void SetTargetPlayerAgent(PlayerAgent targetPlayerAgent)
	{
		IntPtr intPtr = IntPtr.Zero;
		IntPtr intPtr2 = IntPtr.Zero;
		if (TargetPlayerAgent != null)
		{
			intPtr = TargetPlayerAgent.Pointer;
		}
		if (targetPlayerAgent != null)
		{
			intPtr2 = targetPlayerAgent.Pointer;
		}
		if (intPtr != intPtr2)
		{
			TargetPlayerAgent = targetPlayerAgent;
			Update();
		}
	}

	public static float TargetAmmoInPack { get; private set; }
	public static float LocalPlayerAmmoInPack { get; private set; }

	public static void SetInventoryItem()
	{
		ExchangeSlot = InventorySlot.ResourcePack;
		InventorySlot inventorySlot = InventorySlot.ResourcePack;
		AmmoType ammoType = AmmoType.ResourcePackRel;
		if (LocalPlayerWieldingSlot == InventorySlot.Consumable)
		{
			ExchangeSlot = InventorySlot.Consumable;
			inventorySlot = InventorySlot.Consumable;
			ammoType = AmmoType.CurrentConsumable;
		}
		while (true)
		{
			if (TargetPlayerAgent == null || !PlayerBackpackManager.TryGetBackpack(TargetPlayerAgent.Owner, out var backpack))
			{
				goto Invalid;
			}
			bool flag = backpack.TryGetBackpackItem(inventorySlot, out var backpackItem);
			bool flag2 = PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(inventorySlot, out var backpackItem2);
            if (flag)
			{
				TargetAmmoInPack = backpack.AmmoStorage.GetAmmoInPack(ammoType);
				TargetItem = backpackItem.Instance;
			}
			else
			{
				TargetItem = null;
				TargetAmmoInPack = 0f;
			}
			if (flag2)
			{
				LocalPlayerAmmoInPack = PlayerBackpackManager.LocalBackpack.AmmoStorage.GetAmmoInPack(ammoType);
				LocalItem = backpackItem2.Instance;
			}
			else
			{
				LocalItem = null;
				LocalPlayerAmmoInPack = 0f;
			}
			if (flag && flag2)
			{
				goto Exchange;
			}
			if (!flag && flag2)
			{
				goto SourceToTarget;
			}
			if (!flag2 && flag)
			{
				goto TargetToSource;
			}
			if (inventorySlot != InventorySlot.ResourcePack)
			{
				goto Invalid;
			}
			ExchangeSlot = InventorySlot.Consumable;
			inventorySlot = InventorySlot.Consumable;
			ammoType = AmmoType.CurrentConsumable;
		}
	TargetToSource:
		exchangeType = ExchangeType.TargetToSource;
		return;
	Exchange:
		exchangeType = ExchangeType.Exchange;
		return;
	SourceToTarget:
		exchangeType = ExchangeType.SourceToTarget;
		return;
	Invalid:
		exchangeType = ExchangeType.Invalid;
	}

    public static void WantToExchangeItem(SNet_Player target, InventorySlot slot)
	{
        s_ExchangeItemRequestPacket.Ask(new(SNet.LocalPlayer, target, slot));
    }

	public static string GenerateExchangeItemPrompt()
	{
		string prompt;
        float num = (ExchangeSlot == InventorySlot.ResourcePack) ? 20f : 1f;
		switch (exchangeType)
		{
			case ExchangeType.TargetToSource:
				prompt = string.Format(Prompt_TargetToSource,
					TargetPlayerAgent.PlayerName, TargetAmmoInPack / num, TargetItem.ArchetypeName);
				break;
			case ExchangeType.SourceToTarget:
				prompt = string.Format(Prompt_SourceToTarget,
					LocalPlayerAmmoInPack / num, LocalItem.ArchetypeName, TargetPlayerAgent.PlayerName);
				break;
			case ExchangeType.Exchange:
				prompt = string.Format(Prompt_Exchange,
				LocalPlayerAmmoInPack / num, LocalItem.ArchetypeName, TargetPlayerAgent.PlayerName, TargetAmmoInPack / num, TargetItem.ArchetypeName);
				break;
			default:
				prompt = string.Empty;
				break;
		}
		return prompt;
	}

	public static void SetWieldingItem(ItemEquippable wieldingItem, InventorySlot slot, AgentType agentType)
	{
		if (agentType == AgentType.LocalAgent)
		{
			IntPtr intPtr = IntPtr.Zero;
			IntPtr intPtr2 = IntPtr.Zero;
			if (LocalPlayerWieldingItem != null)
			{
				intPtr = LocalPlayerWieldingItem.Pointer;
			}
			if (wieldingItem != null)
			{
				intPtr2 = wieldingItem.Pointer;
			}
			if (intPtr != intPtr2)
			{
				LocalPlayerWieldingItem = wieldingItem;
				LocalPlayerWieldingSlot = slot;
				Update();
				return;
			}
		}
		else
		{
			IntPtr intPtr3 = IntPtr.Zero;
			IntPtr intPtr4 = IntPtr.Zero;
			if (TargetWieldItem != null)
			{
				intPtr3 = TargetWieldItem.Pointer;
			}
			if (wieldingItem != null)
			{
				intPtr4 = wieldingItem.Pointer;
			}
			if (intPtr3 != intPtr4)
			{
				TargetWieldItem = wieldingItem;
				TargetWieldSlot = slot;
				Update();
			}
		}
	}

	public enum ExchangeType : byte
	{
		TargetToSource,
		SourceToTarget,
		Exchange,
        Invalid
    }

	public enum AgentType : byte
    {
		LocalAgent,
		TargetAgent
	}

	public static bool MasterHasExchangeItem;
    public static InventorySlot ExchangeSlot { get; private set; }
    public static LocalPlayerAgent LocalPlayer { get; internal set; }
    private static Item TargetItem;
	private static Item LocalItem;
	private static ItemEquippable TargetWieldItem;
	private static InventorySlot TargetWieldSlot;
	private static ExchangeType exchangeType;
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
