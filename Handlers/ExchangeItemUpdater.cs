using Gear;
using Player;
using System.Linq;
using UnityEngine;
using static Hikaria.ExchangeItem.Managers.ExchangeItemManager;

namespace Hikaria.ExchangeItem.Handlers
{
    internal sealed class ExchangeItemUpdater : MonoBehaviour
    {
        private void Awake()
        {
            DoClear();
        }

        private void OnDestroy()
        {
            DoClear();
        }

        private void FixedUpdate()
        {
            UpdatePlayersInSphere();
            UpdateWieldingItem();
        }

        private void UpdatePlayersInSphere()
        {
            Vector3 position = LocalPlayer.FPSCamera.Position;
            Collider[] array = Physics.OverlapSphere(position, 2.4f, LayerManager.MASK_GIVE_RESOURCE_PACK);
            if (!array.Any())
            {
                SetTargetPlayerAgent(null);
                return;
            }
            Vector3 forward = LocalPlayer.FPSCamera.Forward;
            Vector3 vector;
            float num = 5f;
            PlayerAgent playerAgent = null;
            for (int i = 0; i < array.Length; i++)
            {
                iResourcePackReceiver componentInParent = array[i].GetComponentInParent<iResourcePackReceiver>();
                if (componentInParent != null)
                {
                    PlayerAgent player2 = componentInParent.TryCast<PlayerAgent>();
                    if (player2 != null)
                    {
                        vector = player2.AimTarget.position;
                        if (Vector3.Angle(vector - position, forward) < 45f)
                        {
                            float num2 = Vector3.Distance(vector, position);
                            if (num2 < num)
                            {
                                num = num2;
                                playerAgent = player2;
                            }
                        }
                    }
                }
            }
            SetTargetPlayerAgent(playerAgent);
        }

        private void UpdateWieldingItem()
        {
            PlayerInventoryBase inventory = LocalPlayer.Inventory;
            if (inventory != null)
            {
                InventorySlot wieldedSlot = inventory.WieldedSlot;
                SetWieldingItem(inventory.WieldedItem, wieldedSlot, AgentType.LocalAgent);
            }
            else
            {
                SetWieldingItem(null, InventorySlot.None, AgentType.LocalAgent);
            }
            if (TargetPlayerAgent != null)
            {
                PlayerInventoryBase inventory2 = TargetPlayerAgent.Inventory;
                if (inventory2 != null)
                {
                    InventorySlot wieldedSlot2 = inventory2.WieldedSlot;
                    SetWieldingItem(inventory2.WieldedItem, wieldedSlot2, AgentType.TargetAgent);
                    return;
                }
                SetWieldingItem(null, InventorySlot.None, AgentType.TargetAgent);
            }
        }
    }
}
