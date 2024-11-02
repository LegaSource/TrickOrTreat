using BepInEx.Configuration;

namespace TrickOrTreat.Managers
{
    internal class ConfigManager
    {
        // LITTLE GIRL
        public static ConfigEntry<int> littleGirlRarity;
        public static ConfigEntry<int> littleGirlRarityIncrement;
        public static ConfigEntry<int> waitingDuration;
        // HALLOWEEN CANDY
        public static ConfigEntry<int> halloweenCandyRarity;
        public static ConfigEntry<int> minHalloweenCandy;
        public static ConfigEntry<int> maxHalloweenCandy;

        internal static void Load()
        {
            littleGirlRarity = TrickOrTreat.configFile.Bind(Constants.LITTLE_GIRL, "Rarity", 5, "Little Girl base rarity.");
            littleGirlRarityIncrement = TrickOrTreat.configFile.Bind(Constants.LITTLE_GIRL, "Increment rarity", 3, "By how much the Little Girl's chances of spawning are increased for each Halloween Candy spawned?");
            waitingDuration = TrickOrTreat.configFile.Bind(Constants.LITTLE_GIRL, "Waiting duration", 10, "Time window during which a player can give a candy to the little girl before she sends her curse.");
            halloweenCandyRarity = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Rarity", 50, Constants.HALLOWEEN_CANDY + " spawn rarity.");
            minHalloweenCandy = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Min spawn", 2, "Min " + Constants.HALLOWEEN_CANDY + " to spawn");
            maxHalloweenCandy = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Max spawn", 6, "Max " + Constants.HALLOWEEN_CANDY + " to spawn");
        }
    }
}
