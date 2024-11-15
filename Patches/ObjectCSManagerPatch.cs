using CursedScraps.Managers;
using HarmonyLib;
using System.Linq;
using TrickOrTreat.Behaviours;

namespace TrickOrTreat.Patches
{
    internal class ObjectCSManagerPatch
    {
        [HarmonyPatch(typeof(ObjectCSManager), nameof(ObjectCSManager.AddNewItems))]
        [HarmonyPostfix]
        private static void ChangeRarity()
        {
            SpawnableEnemyWithRarity spawnableEnemies = RoundManager.Instance.currentLevel.Enemies.FirstOrDefault(e => e.enemyType.enemyName.Equals(Constants.LITTLE_GIRL));
            if (spawnableEnemies != null)
            {
                int countCandies = UnityEngine.Object.FindObjectsOfType<HalloweenCandy>().Count();
                spawnableEnemies.rarity = Managers.ConfigManager.littleGirlRarity.Value + countCandies * Managers.ConfigManager.littleGirlRarityIncrement.Value;
            }
        }
    }
}
