using BepInEx.Configuration;

namespace TrickOrTreat.Managers
{
    internal class ConfigManager
    {
        // Little Girl
        public static ConfigEntry<int> littleGirlRarity;
        public static ConfigEntry<int> littleGirlRarityIncrement;
        public static ConfigEntry<int> waitingDuration;
        // Halloween Candy
        public static ConfigEntry<bool> isHalloweenCandyIconOn;
        public static ConfigEntry<float> halloweenCandyPosX;
        public static ConfigEntry<float> halloweenCandyPosY;
        public static ConfigEntry<int> halloweenCandyRarity;
        public static ConfigEntry<int> minHalloweenCandy;
        public static ConfigEntry<int> maxHalloweenCandy;

        internal static void Load()
        {
            // Little Girl
            littleGirlRarity = TrickOrTreat.configFile.Bind(Constants.LITTLE_GIRL, "Rarity", 5, "Little Girl base rarity.");
            littleGirlRarityIncrement = TrickOrTreat.configFile.Bind(Constants.LITTLE_GIRL, "Increment rarity", 3, "By how much the Little Girl's chances of spawning are increased for each Halloween Candy spawned?");
            waitingDuration = TrickOrTreat.configFile.Bind(Constants.LITTLE_GIRL, "Waiting duration", 5, "Time window during which a player can give a candy to the little girl before she sends her curse.");
            // Halloween Candy
            isHalloweenCandyIconOn = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Is icon displayed", true, $"Display the {Constants.HALLOWEEN_CANDY} icon when player has one");
            halloweenCandyPosX = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Dead curses pos X", -30f, $"X position of the {Constants.HALLOWEEN_CANDY} icon");
            halloweenCandyPosY = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Dead curses pos Y", 40f, $"Y position of the {Constants.HALLOWEEN_CANDY} icon");
            halloweenCandyRarity = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Rarity", 50, $"{Constants.HALLOWEEN_CANDY} spawn rarity.");
            minHalloweenCandy = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Min spawn", 3, $"Min {Constants.HALLOWEEN_CANDY} to spawn");
            maxHalloweenCandy = TrickOrTreat.configFile.Bind(Constants.HALLOWEEN_CANDY, "Max spawn", 4, $"Max {Constants.HALLOWEEN_CANDY} to spawn");
        }
    }
}
