using BepInEx.Configuration;

namespace TrickOrTreat.Managers
{
    internal class ConfigManager
    {
        // LITTLE GIRL
        public static ConfigEntry<int> littleGirlRarity;
        public static ConfigEntry<int> waitingDuration;
        // HALLOWEEN CANDY
        public static ConfigEntry<int> halloweenCandyRarity;
        public static ConfigEntry<int> minHalloweenCandy;
        public static ConfigEntry<int> maxHalloweenCandy;

        internal static void Load()
        {
            littleGirlRarity = TrickOrTreat.configFile.Bind(Constants.LITTLE_GIRL, "Rarity", 10, "Little Girl rarity.");
            waitingDuration = TrickOrTreat.configFile.Bind(Constants.LITTLE_GIRL, "Waiting duration", 10, "Time window during which a player can give a candy to the little girl before she sends her curse.");
            halloweenCandyRarity = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Rarity", 20, Constants.HALLOWEEN_CANDY + " spawn rarity.");
            minHalloweenCandy = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Min spawn", 3, "Min " + Constants.HALLOWEEN_CANDY + " to spawn");
            maxHalloweenCandy = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Max spawn", 6, "Max " + Constants.HALLOWEEN_CANDY + " to spawn");
        }
    }
}
