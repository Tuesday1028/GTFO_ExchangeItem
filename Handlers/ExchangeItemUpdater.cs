using Gear;
using Hikaria.ExchangeItem.Managers;
using Localization;
using Player;
using UnityEngine;

namespace Hikaria.ExchangeItem.Handlers
{
    internal sealed class ExchangeItemUpdater : MonoBehaviour
    {
        private ResourcePackFirstPerson m_wieldResourcePack;
        private bool m_lastKeyDown;
        private bool m_keyReleased = true;

        private bool m_localItemInfAmmo;
        private bool m_targetItemInfAmmo;

        private int m_localItemTimes;
        private int m_receiverItemTimes;

        private bool m_localHasResourcePack;
        private bool m_localHasConsumable;
        private bool m_receiverHasResourcePack;
        private bool m_receiverHasConsumable;

        private bool m_allowGive;
        private bool m_allowGet;
        private bool m_allowExchange;
        private bool m_allowGetResourcePack;
        private bool m_allowGetConsumable;

        private BackpackItem m_receiverConsumableBPItem;
        private BackpackItem m_localConsumableBPItem;
        private BackpackItem m_receiverResourcePackBPItem;
        private BackpackItem m_localResourcePackBPItem;

        private PlayerBackpack m_receiverBackpack;

        private InventorySlot m_localWieldSlot;

        private void Awake()
        {
            m_localPlayer = GetComponent<LocalPlayerAgent>();
            m_interactExchangeItem = GetComponent<Core.Components.Interact_ManualTimedWithCallback>() ?? gameObject.AddComponent<Core.Components.Interact_ManualTimedWithCallback>();
            m_interactExchangeItem.InteractDuration = m_interactionDuration;
            m_interactExchangeItem.AbortOnDotOrDistanceDiff = false;
            m_interactExchangeItem.OnTrigger = DoExchangeItem;
            UpdateInteractionActionName();
        }

        private void Update()
        {
            if (!ExchangeItemManager.MasterHasExchangeItem || m_localPlayer.Interaction.HasWorldInteraction
                || (!m_localPlayer.Inventory.WieldedItem?.AllowPlayerInteraction ?? false)
                || (m_wieldResourcePack?.m_interactApplyResource.TimerIsActive ?? false)
                || !m_localPlayer.Alive)
            {
                if (m_interactExchangeItem.TimerIsActive)
                    m_interactExchangeItem.PlayerSetSelected(false, m_localPlayer);

                return;
            }

            if (!m_localPlayer.Interaction.HasWorldInteraction && !m_localPlayer.FPItemHolder.ItemHiddenTrigger)
            {
                UpdateInteraction();
            }
        }

        public void OnWieldItemChanged()
        {
            if (m_interactExchangeItem.IsSelected)
            {
                m_interactExchangeItem.PlayerSetSelected(false, m_localPlayer);
            }
            m_localWieldSlot = m_localPlayer.Inventory.WieldedSlot;
            m_wieldResourcePack = m_localPlayer.Inventory.WieldedItem?.TryCast<ResourcePackFirstPerson>();
        }

        public void OnInteractionKeyChanged()
        {
            UpdateInteractionActionName();
        }

        public void OnAmmoStorageChanged(PlayerAgent player)
        {
            if (m_interactExchangeItem.IsSelected)
            {
                if (player.IsLocallyOwned || player.GlobalID == m_actionReceiver.GlobalID)
                {
                    m_interactExchangeItem.PlayerSetSelected(false, m_localPlayer);
                }
            }
        }

        private void UpdateInteractionActionName(string targetName = "")
        {
            m_interactExchangeItem.SetAction(targetName, Features.ExchangeItem.Settings.ExchangeItemKey);
        }

        private bool UpdateInteraction()
        {
            if (!m_interactExchangeItem.TimerIsActive)
            {
                m_actionReceiver = null;
                Vector3 position = m_localPlayer.FPSCamera.Position;
                Vector3 forward = m_localPlayer.FPSCamera.Forward;
                if (Physics.Raycast(position, forward, out var rayHit, 2.4f, LayerManager.MASK_GIVE_RESOURCE_PACK))
                {
                    iResourcePackReceiver componentInParent = rayHit.collider.GetComponentInParent<iResourcePackReceiver>();
                    if (componentInParent != null)
                    {
                        var player = componentInParent.TryCast<PlayerAgent>();
                        if (player != null)
                        {
                            m_actionReceiver = player;
                        }
                    }
                }
            }

            return UpdateInteractionDetails();
        }

        private bool UpdateInteractionDetails()
        {
            if (!m_keyReleased)
            {
                m_keyReleased = !Input.GetKey(m_interactExchangeItem.InputKey);
                if (!m_keyReleased)
                {
                    m_interactExchangeItem.PlayerSetSelected(false, m_localPlayer);
                    return false;
                }
            }

            if (!m_actionReceiver || !PlayerBackpackManager.TryGetBackpack(m_actionReceiver.Owner, out m_receiverBackpack))
            {
                m_exchangeType = ExchangeType.Invalid;
                m_exchangeSlot = InventorySlot.None;

                m_interactExchangeItem.PlayerSetSelected(false, m_localPlayer);
                return false;
            }

            m_localHasResourcePack = PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(InventorySlot.ResourcePack, out m_localResourcePackBPItem) && m_localResourcePackBPItem.Instance != null;
            m_localHasConsumable = PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(InventorySlot.Consumable, out m_localConsumableBPItem) && m_localConsumableBPItem.Instance != null;
            m_receiverHasResourcePack = m_receiverBackpack.TryGetBackpackItem(InventorySlot.ResourcePack, out m_receiverResourcePackBPItem) && m_receiverResourcePackBPItem.Instance != null;
            m_receiverHasConsumable = m_receiverBackpack.TryGetBackpackItem(InventorySlot.Consumable, out m_receiverConsumableBPItem) && m_receiverConsumableBPItem.Instance != null;

            m_allowGive = (m_localWieldSlot == InventorySlot.ResourcePack && !m_receiverHasResourcePack) || (m_localWieldSlot == InventorySlot.Consumable && !m_receiverHasConsumable);
            m_allowExchange = ((m_localWieldSlot == InventorySlot.ResourcePack && m_receiverHasResourcePack) || (m_localWieldSlot == InventorySlot.Consumable && m_receiverHasConsumable));
            m_allowGetConsumable = !m_localHasConsumable && m_receiverHasConsumable;
            m_allowGetResourcePack = !m_localHasResourcePack && m_receiverHasResourcePack;
            m_allowGet = m_allowGetConsumable || m_allowGetResourcePack;

            if (m_allowExchange)
            {
                m_exchangeSlot = m_localWieldSlot;
                m_exchangeType = ExchangeType.Exchange;
            }
            else if (m_allowGive)
            {
                m_exchangeSlot = m_localWieldSlot;
                m_exchangeType = ExchangeType.Give;
            }
            else if (m_allowGet)
            {
                m_exchangeSlot = m_allowGetResourcePack ? InventorySlot.ResourcePack : InventorySlot.Consumable;
                m_exchangeType = ExchangeType.Get;
            }
            else
            {
                m_exchangeSlot = InventorySlot.None;
                m_exchangeType = ExchangeType.Invalid;
            }

            if (m_exchangeSlot == InventorySlot.ResourcePack)
            {
                m_localItem = m_localHasResourcePack ? m_localResourcePackBPItem.Instance : null;
                m_targetItem = m_receiverHasResourcePack ? m_receiverResourcePackBPItem.Instance : null;
            }
            else if (m_exchangeSlot == InventorySlot.Consumable)
            {
                m_localItem = m_localHasConsumable ? m_localConsumableBPItem.Instance : null;
                m_targetItem = m_receiverHasConsumable ? m_receiverConsumableBPItem.Instance : null;
            }
            else
            {
                m_localItem = null;
                m_targetItem = null;
            }


            m_localItemInfAmmo = m_localItem?.ItemDataBlock?.GUIShowAmmoInfinite ?? false;
            m_targetItemInfAmmo = m_targetItem?.ItemDataBlock?.GUIShowAmmoInfinite ?? false;
            m_localItemTimes = PlayerBackpackManager.LocalBackpack.AmmoStorage.GetBulletsInPack(PlayerAmmoStorage.GetAmmoTypeFromSlot(m_exchangeSlot));
            m_receiverItemTimes = m_receiverBackpack.AmmoStorage.GetBulletsInPack(PlayerAmmoStorage.GetAmmoTypeFromSlot(m_exchangeSlot));

            if (m_exchangeType == ExchangeType.Exchange
                && m_receiverItemTimes == m_localItemTimes && m_localItem.ItemDataBlock.persistentID == m_targetItem.ItemDataBlock.persistentID)
            {
                m_exchangeType = ExchangeType.Invalid;
            }

            switch (m_exchangeType)
            {
                case ExchangeType.Get:
                    m_interactExchangeItem.InteractionMessage = string.Format(Prompt_Get,
                        m_actionReceiver.GetColoredName(), m_localItemInfAmmo ? string.Empty : $" {string.Format(Prompt_Times, m_receiverItemTimes)}{(Features.ExchangeItem.Localization.CurrentLanguage != TheArchive.Core.Localization.Language.Chinese && m_receiverItemTimes > 1 ? "s" : string.Empty)}", m_targetItem.ArchetypeName);
                    break;
                case ExchangeType.Give:
                    m_interactExchangeItem.InteractionMessage = string.Format(Prompt_Give,
                        m_targetItemInfAmmo ? string.Empty : $" {string.Format(Prompt_Times, m_localItemTimes)}{(Features.ExchangeItem.Localization.CurrentLanguage != TheArchive.Core.Localization.Language.Chinese && m_localItemTimes > 1 ? "s" : string.Empty)}", m_localItem.ArchetypeName, m_actionReceiver.GetColoredName());
                    break;
                case ExchangeType.Exchange:
                    m_interactExchangeItem.InteractionMessage = string.Format(Prompt_Exchange,
                    m_targetItemInfAmmo ? string.Empty : $" {string.Format(Prompt_Times, m_localItemTimes)}{(Features.ExchangeItem.Localization.CurrentLanguage != TheArchive.Core.Localization.Language.Chinese && m_localItemTimes > 1 ? "s" : string.Empty)}", m_localItem.ArchetypeName, m_actionReceiver.GetColoredName(), m_localItemInfAmmo ? string.Empty : $" {string.Format(Prompt_Times, m_receiverItemTimes)}{(Features.ExchangeItem.Localization.CurrentLanguage != TheArchive.Core.Localization.Language.Chinese && m_receiverItemTimes > 1 ? "s" : string.Empty)}", m_targetItem.ArchetypeName);
                    break;
                default:
                    break;
            }

            var flag = m_exchangeType != ExchangeType.Invalid;
            var preIsActive = m_interactExchangeItem.TimerIsActive;
            m_interactExchangeItem.ManualUpdateWithCondition(flag, m_localPlayer, flag);
            if (flag && !preIsActive && m_interactExchangeItem.TimerIsActive)
            {
                m_localPlayer.Sync.SendGenericInteract(m_exchangeType == ExchangeType.Give ? pGenericInteractAnimation.TypeEnum.GiveResource : GetReachHeight(), false);

                m_interactExchangeItem.ForceUpdatePrompt();
            }

            return true;
        }

        private pGenericInteractAnimation.TypeEnum GetReachHeight()
        {
            float y = m_actionReceiver.Position.y;
            float num = m_localPlayer.Position.y;
            num += Mathf.Lerp(1.6f, 1.1f, m_localPlayer.Locomotion.GetCrouch());
            float num2 = y - num;
            if (num2 > 0.25f)
            {
                return pGenericInteractAnimation.TypeEnum.PickUpHigh;
            }
            if (num2 > -0.35f)
            {
                return pGenericInteractAnimation.TypeEnum.PickUpMedium;
            }
            return pGenericInteractAnimation.TypeEnum.PickUpLow;
        }

        private void DoExchangeItem()
        {
            m_keyReleased = false;
            ExchangeItemManager.WantToExchangeItem(m_actionReceiver.Owner, m_exchangeSlot);
        }

        private PlayerAgent m_actionReceiver;

        private Item m_localItem;
        private Item m_targetItem;

        private LocalPlayerAgent m_localPlayer;

        private ExchangeType m_exchangeType;
        private InventorySlot m_exchangeSlot;

        private Core.Components.Interact_ManualTimedWithCallback m_interactExchangeItem;

        public string Prompt_Exchange => Features.ExchangeItem.Localization.Get(1);
        public string Prompt_Get => Features.ExchangeItem.Localization.Get(2);
        public string Prompt_Give => Features.ExchangeItem.Localization.Get(3);
        public string Prompt_Times => Features.ExchangeItem.Localization.Get(4);

        private float m_interactionDuration = 0.6f;
    }

    public enum ExchangeType : byte
    {
        Get,
        Give,
        Exchange,
        Invalid
    }
}
