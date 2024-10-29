using HarmonyLib;
using System.Linq;
using TrickOrTreat.Behaviours;

namespace TrickOrTreat.Patches
{
    internal class BeltBagInventoryUIPatch
    {
        [HarmonyPatch(typeof(BeltBagInventoryUI), nameof(BeltBagInventoryUI.RemoveItemFromUI))]
        [HarmonyPrefix]
        private static bool PreDropObject(ref BeltBagInventoryUI __instance, int slot)
        {
            if (__instance.currentBeltBag != null && slot != -1)
            {
                if (__instance.currentBeltBag.objectsInBag.Count > slot
                    && __instance.currentBeltBag.objectsInBag[slot] != null
                    && __instance.currentBeltBag.objectsInBag[slot] is HalloweenCandy halloweenCandy
                    && halloweenCandy.currentStackedItems > 1
                    && !__instance.currentBeltBag.tryingAddToBag)
                {
                    halloweenCandy.currentStackedItems--;
                    __instance.inventorySlotIcons[slot].sprite = TrickOrTreat.sprites.FirstOrDefault(s => s.name.Equals($"HalloweenCandy{halloweenCandy.currentStackedItems}"));
                    halloweenCandy.SpawnObjectServerRpc((int)__instance.currentBeltBag.playerHeldBy.playerClientId);

                    HUDManager.Instance.SetMouseCursorSprite(HUDManager.Instance.handOpenCursorTex);
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(BeltBagInventoryUI), nameof(BeltBagInventoryUI.FillSlots))]
        [HarmonyPostfix]
        private static void FillSlots(ref BeltBagInventoryUI __instance)
        {
            for (int i = 0; i < __instance.inventorySlots.Length; i++)
            {
                if (__instance.currentBeltBag.objectsInBag == null || __instance.currentBeltBag.objectsInBag.Count <= i || __instance.currentBeltBag.objectsInBag[i] == null)
                {
                    __instance.inventorySlotIcons[i].enabled = false;
                    continue;
                }

                if (__instance.currentBeltBag.objectsInBag[i] is HalloweenCandy halloweenCandy && halloweenCandy.currentStackedItems > 1)
                {
                    __instance.inventorySlotIcons[i].enabled = true;
                    __instance.inventorySlotIcons[i].sprite = TrickOrTreat.sprites.FirstOrDefault(s => s.name.Equals($"HalloweenCandy{halloweenCandy.currentStackedItems}"));
                }
            }
        }
    }
}
