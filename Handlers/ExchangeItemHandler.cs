using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hikaria.ExchangeItem.Managers;
using Il2CppInterop.Runtime.Attributes;
using Localization;
using Player;
using System.Collections;
using UnityEngine;

namespace Hikaria.ExchangeItem.Handlers
{
    internal class ExchangeItemHandler : MonoBehaviour
    {
        private void Awake()
        {
            ExchangeItemManager.OnUpdated += ExchangeItemManager_OnUpdated;
        }

        private void OnDestroy()
        {
            ExchangeItemManager.OnUpdated -= ExchangeItemManager_OnUpdated;
        }

        private void Update()
        {
            if (InteractionAllowed && !HoldingKey)
            {
                if (Input.GetKeyDown(ExchangeItemKey))
                {
                    StartRoutine();
                    HoldingKey = true;
                    return;
                }
            }
            else if (Input.GetKeyUp(ExchangeItemKey))
            {
                StopRoutine();
                HoldingKey = false;
            }
        }

        [HideFromIl2Cpp]
        private void ExchangeItemManager_OnUpdated()
        {
            InteractionAllowed = ExchangeItemManager.InteractionAllowed;
            if (InteractionAllowed)
            {
                UpdateExchangeItemPrompt();
                SetExchangeItemPrompt();
                return;
            }
            if (ExchangeItemManager.LocalPlayerWieldingSlot != InventorySlot.ResourcePack)
            {
                GuiManager.InteractionLayer.InteractPromptVisible = false;
                GuiManager.InteractionLayer.SetInteractPrompt(string.Empty, string.Empty, ePUIMessageStyle.Default);
            }
        }

        [HideFromIl2Cpp]
        private static void UpdateExchangeItemPrompt()
        {
            InteractionPrompt = ExchangeItemManager.GenerateExchangeItemPrompt();
            ExchangeItemButtonText = string.Format(Text.Get(827), ExchangeItemKey);
        }

        private static void SetExchangeItemPrompt()
        {
            GuiManager.InteractionLayer.InteractPromptVisible = true;
            GuiManager.InteractionLayer.SetInteractPrompt(InteractionPrompt, ExchangeItemButtonText, ePUIMessageStyle.Default);
        }

        [HideFromIl2Cpp]
        private void StartRoutine()
        {
            StopRoutine();
            InteractionCoroutine = StartCoroutine(Interaction(0.4f).WrapToIl2Cpp());
            StartInteraction();
        }

        [HideFromIl2Cpp]
        private void StopRoutine()
        {
            if (InteractionCoroutine != null)
            {
                StopCoroutine(InteractionCoroutine);
                StopInteraction();
            }
        }

        [HideFromIl2Cpp]
        private static void StartInteraction()
        {
            GuiManager.InteractionLayer.InteractPromptVisible = true;
        }

        [HideFromIl2Cpp]
        private static void StopInteraction()
        {
            GuiManager.InteractionLayer.InteractPromptVisible = false;
        }

        [HideFromIl2Cpp]
        private IEnumerator Interaction(float interactionTime)
        {
            SetExchangeItemPrompt();
            float timer = 0f;
            bool timerInterrupted = false;
            while (timer <= interactionTime)
            {
                if (!ExchangeItemManager.InteractionAllowed)
                {
                    timerInterrupted = true;
                    break;
                }
                SetExchangeItemPrompt();
                GuiManager.InteractionLayer.SetTimer(timer / interactionTime);
                timer += Time.deltaTime;
                yield return null;
            }
            if (!timerInterrupted)
            {
                ExchangeItemManager.WantToExchangeItem(ExchangeItemManager.TargetPlayerAgent.Owner, ExchangeItemManager.ExchangeSlot);
            }
            StopInteraction();
            InteractionCoroutine = null;
            yield break;
        }

        public static KeyCode ExchangeItemKey = KeyCode.T;
        private bool HoldingKey;
        private static string ExchangeItemButtonText = string.Empty;
        private static string InteractionPrompt = string.Empty;
        private bool InteractionAllowed;
        private Coroutine InteractionCoroutine;
    }
}
