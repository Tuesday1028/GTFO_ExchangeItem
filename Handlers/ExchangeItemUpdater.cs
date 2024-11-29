using Gear;
using Hikaria.ExchangeItem.Managers;
using Player;
using UnityEngine;

namespace Hikaria.ExchangeItem.Handlers
{
    internal sealed class ExchangeItemUpdater : MonoBehaviour
    {
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
            if (!m_localPlayer.Interaction.HasWorldInteraction && !m_localPlayer.FPItemHolder.ItemHiddenTrigger)
            {
                UpdateInteraction();
                return;
            }
            if (m_interactExchangeItem.TimerIsActive)
            {
                m_interactExchangeItem.PlayerSetSelected(false, m_localPlayer);
            }
        }

        public void OnWieldItemChanged()
        {
            if (m_interactExchangeItem.IsSelected)
            {
                m_interactExchangeItem.PlayerSetSelected(false, m_localPlayer);
            }
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

        private void UpdateInteraction()
        {
            if (!ExchangeItemManager.MasterHasExchangeItem 
                || m_localPlayer.Interaction.HasWorldInteraction || (!m_localPlayer.Inventory.WieldedItem?.AllowPlayerInteraction ?? false) || !m_localPlayer.Alive)
                return;

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

            UpdateInteractionDetails();
        }

        private bool UpdateInteractionDetails()
        {
            if (!m_actionReceiver || !PlayerBackpackManager.TryGetBackpack(m_actionReceiver.Owner, out var receiverBackpack))
            {
                m_exchangeType = ExchangeType.Invalid;
                m_exchangeSlot = InventorySlot.None;

                m_interactExchangeItem.PlayerSetSelected(false, m_localPlayer);
                return false;
            }

            var localBackpack = PlayerBackpackManager.LocalBackpack;

            var localHasResourcePack = localBackpack.TryGetBackpackItem(InventorySlot.ResourcePack, out var localResourcePackBPItem) && localResourcePackBPItem.Instance != null;
            var localHasConsumable = localBackpack.TryGetBackpackItem(InventorySlot.Consumable, out var localConsumableBPItem) && localConsumableBPItem.Instance != null;
            var receiverHasResourcePack = receiverBackpack.TryGetBackpackItem(InventorySlot.ResourcePack, out var receiverResourcePackBPItem) && receiverResourcePackBPItem.Instance != null;
            var receiverHasConsumable = receiverBackpack.TryGetBackpackItem(InventorySlot.Consumable, out var receiverConsumableBPItem) && receiverConsumableBPItem.Instance != null;

            var wieldSlot = m_localPlayer.Inventory.WieldedSlot;

            var allowGive = (wieldSlot == InventorySlot.ResourcePack && !receiverHasResourcePack) || (wieldSlot == InventorySlot.Consumable && !receiverHasConsumable);
            var allowExchange = (wieldSlot == InventorySlot.ResourcePack && receiverHasResourcePack) || (wieldSlot == InventorySlot.Consumable && receiverHasConsumable);
            var allowGetConsumable = !localHasConsumable && receiverHasConsumable;
            var allowGetResourcePack = !localHasResourcePack && receiverHasResourcePack;
            var allowGet = allowGetConsumable || allowGetResourcePack;

            if (allowExchange)
            {
                m_exchangeSlot = wieldSlot;
                m_exchangeType = ExchangeType.Exchange;
            }
            else if (allowGive)
            {
                m_exchangeSlot = wieldSlot;
                m_exchangeType = ExchangeType.Give;
            }
            else if (allowGet)
            {
                m_exchangeSlot = allowGetResourcePack ? InventorySlot.ResourcePack : InventorySlot.Consumable;
                m_exchangeType = ExchangeType.Get;
            }
            else
            {
                m_exchangeSlot = InventorySlot.None;
                m_exchangeType = ExchangeType.Invalid;
            }

            if (m_exchangeSlot == InventorySlot.ResourcePack)
            {
                m_localItem = localHasResourcePack ? localResourcePackBPItem.Instance : null;
                m_targetItem = receiverHasResourcePack ? receiverResourcePackBPItem.Instance : null;
            }
            else if (m_exchangeSlot == InventorySlot.Consumable)
            {
                m_localItem = localHasConsumable ? localConsumableBPItem.Instance : null;
                m_targetItem = receiverHasConsumable ? receiverConsumableBPItem.Instance : null;
            }
            else
            {
                m_localItem = null;
                m_targetItem = null;
            }

            var localItemInfAmmo = m_localItem?.ItemDataBlock?.GUIShowAmmoInfinite ?? false;
            var targetItemInfAmmo = m_targetItem?.ItemDataBlock?.GUIShowAmmoInfinite ?? false;
            var receiverItemTimes = receiverBackpack.AmmoStorage.GetBulletsInPack(PlayerAmmoStorage.GetAmmoTypeFromSlot(m_exchangeSlot));
            var localItemTimes = localBackpack.AmmoStorage.GetBulletsInPack(PlayerAmmoStorage.GetAmmoTypeFromSlot(m_exchangeSlot));

            string receiverTimes = targetItemInfAmmo ? string.Empty : $" {string.Format(Prompt_Times, receiverItemTimes)}{(Features.ExchangeItem.Localization.CurrentLanguage != TheArchive.Core.Localization.Language.Chinese && receiverItemTimes > 1 ? "s" : string.Empty)}";
            string localTimes = localItemInfAmmo ? string.Empty : $" {string.Format(Prompt_Times, localItemTimes)}{(Features.ExchangeItem.Localization.CurrentLanguage != TheArchive.Core.Localization.Language.Chinese && localItemTimes > 1 ? "s" : string.Empty)}";

            switch (m_exchangeType)
            {
                case ExchangeType.Get:
                    m_interactExchangeItem.InteractionMessage = string.Format(Prompt_Get,
                        m_actionReceiver.GetColoredName(), receiverTimes, m_targetItem.ArchetypeName);
                    break;
                case ExchangeType.Give:
                    m_interactExchangeItem.InteractionMessage = string.Format(Prompt_Give,
                        localTimes, m_localItem.ArchetypeName, m_actionReceiver.GetColoredName());
                    break;
                case ExchangeType.Exchange:
                    m_interactExchangeItem.InteractionMessage = string.Format(Prompt_Exchange,
                    localTimes, m_localItem.ArchetypeName, m_actionReceiver.GetColoredName(), receiverTimes, m_targetItem.ArchetypeName);
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
