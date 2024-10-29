using GameNetcodeStuff;
using HarmonyLib;
using TrickOrTreat.Behaviours;
using UnityEngine;

namespace TrickOrTreat.Patches
{
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.BeginGrabObject))]
        [HarmonyPrefix]
        private static bool PreGrabObject(ref PlayerControllerB __instance)
        {
            __instance.interactRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
            if (!Physics.Raycast(__instance.interactRay, out __instance.hit, __instance.grabDistance, __instance.interactableObjectsMask) || __instance.hit.collider.gameObject.layer == 8 || !(__instance.hit.collider.tag == "PhysicsProp") || __instance.twoHanded || __instance.sinkingValue > 0.73f || Physics.Linecast(__instance.gameplayCamera.transform.position, __instance.hit.collider.transform.position + __instance.transform.up * 0.16f, 1073741824, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
            GrabbableObject grabbableObject = __instance.hit.collider.transform.gameObject.GetComponent<GrabbableObject>();
            if (grabbableObject != null && grabbableObject is HalloweenCandy halloweenCandy)
            {
                for (int i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    if (__instance.ItemSlots[i] != null && __instance.ItemSlots[i] is HalloweenCandy halloweenCandyHolded && halloweenCandyHolded.currentStackedItems < 5)
                    {
                        halloweenCandyHolded.currentStackedItems++;
                        halloweenCandy.UpdateIcon(i, halloweenCandyHolded.currentStackedItems);
                        halloweenCandy.DestroyObjectServerRpc();
                        return false;
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DiscardHeldObject))]
        [HarmonyPrefix]
        private static bool PreDropObject(ref PlayerControllerB __instance)
        {
            if (__instance.currentlyHeldObjectServer is HalloweenCandy halloweenCandy && halloweenCandy.currentStackedItems > 1)
            {
                halloweenCandy.currentStackedItems--;
                halloweenCandy.UpdateIcon(__instance.currentItemSlot, halloweenCandy.currentStackedItems);
                halloweenCandy.SpawnObjectServerRpc((int)__instance.playerClientId);
                return false;
            }
            return true;
        }
    }
}
