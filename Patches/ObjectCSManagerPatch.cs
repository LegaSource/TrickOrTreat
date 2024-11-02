using CursedScraps.Managers;
using HarmonyLib;
using System.Linq;
using TrickOrTreat.Behaviours;
using TrickOrTreat.Managers;

namespace TrickOrTreat.Patches
{
    internal class ObjectCSManagerPatch
    {
        [HarmonyPatch(typeof(ObjectCSManager), nameof(ObjectCSManager.AddNewItems))]
        [HarmonyPostfix]
        private static void ChangeRarity()
        {
            int countCandies = UnityEngine.Object.FindObjectsOfType<HalloweenCandy>().Count();
            RoundManager.Instance.currentLevel.Enemies.FirstOrDefault(e => e.enemyType.enemyName.Equals(Constants.LITTLE_GIRL)).rarity = ConfigManager.littleGirlRarity.Value + countCandies * ConfigManager.littleGirlRarityIncrement.Value;
        }
    }
}
