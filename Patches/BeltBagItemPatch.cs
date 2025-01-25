using HarmonyLib;
using TrickOrTreat.Behaviours;

namespace TrickOrTreat.Patches
{
    internal class BeltBagItemPatch
    {
        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.PutObjectInBagLocalClient))]
        [HarmonyPrefix]
        private static bool PreGrabObject(ref GrabbableObject gObject)
        {
            if (gObject is HalloweenCandy halloweenCandy)
            {
                PlayerControllerBPatch.AddHalloweenCandy(halloweenCandy);
                return false;
            }
            return true;
        }
    }
}
