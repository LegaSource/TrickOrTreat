using HarmonyLib;

namespace TrickOrTreat.Patches
{
    internal class RoundManagerPatch
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevel))]
        [HarmonyPostfix]
        private static void StartGame()
            => ResetStackedCandies();

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DetectElevatorIsRunning))]
        [HarmonyPostfix]
        private static void EndGame()
            => ResetStackedCandies();

        public static void ResetStackedCandies()
        {
            PlayerControllerBPatch.currentStackedCandies = 0;
            HUDManagerPatch.SetActive(false);
        }
    }
}
