using GameNetcodeStuff;
using HarmonyLib;
using TrickOrTreat.Behaviours;
using UnityEngine;

namespace TrickOrTreat.Patches
{
    internal class PlayerControllerBPatch
    {
        public static int currentStackedCandies = 0;

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
                AddHalloweenCandy(halloweenCandy);
                return false;
            }

            return true;
        }

        public static void AddHalloweenCandy(HalloweenCandy halloweenCandy)
        {
            halloweenCandy.DestroyObjectServerRpc();
            currentStackedCandies++;
            HUDManagerPatch.SetActive(true);
        }
    }
}
