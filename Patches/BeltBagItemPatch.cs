using HarmonyLib;
using System.Linq;
using TrickOrTreat.Behaviours;

namespace TrickOrTreat.Patches
{
    internal class BeltBagItemPatch
    {
        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.PutObjectInBagLocalClient))]
        [HarmonyPrefix]
        private static bool PreGrabObject(ref BeltBagItem __instance, ref GrabbableObject gObject)
        {
            if (gObject is HalloweenCandy halloweenCandy)
            {
                for (int i = 0; i < __instance.beltBagUI.inventorySlots.Length; i++)
                {
                    if (__instance.objectsInBag == null || __instance.objectsInBag.Count <= i || __instance.objectsInBag[i] == null)
                    {
                        __instance.beltBagUI.inventorySlotIcons[i].enabled = false;
                        continue;
                    }
                    
                    if (__instance.objectsInBag[i] is HalloweenCandy halloweenCandyHolded && halloweenCandyHolded.currentStackedItems < 5)
                    {
                        halloweenCandyHolded.currentStackedItems++;
                        __instance.beltBagUI.inventorySlotIcons[i].enabled = true;
                        __instance.beltBagUI.inventorySlotIcons[i].sprite = TrickOrTreat.sprites.FirstOrDefault(s => s.name.Equals($"HalloweenCandy{halloweenCandyHolded.currentStackedItems}"));
                        halloweenCandy.DestroyObjectServerRpc();

                        RoundManager.PlayRandomClip(__instance.bagAudio, __instance.grabItemInBagSFX, randomize: true, 1f, -1);
                        __instance.StartCoroutine(__instance.putObjectInBagAnimation(gObject));

                        return false;
                    }
                }
            }
            return true;
        }
    }
}
