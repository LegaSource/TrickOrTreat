using BepInEx.Configuration;

namespace TrickOrTreat.Managers;

internal class ConfigManager
{
    // Hollow Girl
    public static ConfigEntry<int> hollowGirlRarity;
    public static ConfigEntry<int> waitingDuration;
    public static ConfigEntry<int> damage;
    // Cursed Candy
    public static ConfigEntry<int> cursedCandyRarity;
    public static ConfigEntry<int> minCursedCandy;
    public static ConfigEntry<int> maxCursedCandy;

    internal static void Load()
    {
        // Hollow Girl
        hollowGirlRarity = TrickOrTreat.configFile.Bind(Constants.HOLLOW_GIRL, "Rarity", 20, $"{Constants.HOLLOW_GIRL} base rarity.");
        waitingDuration = TrickOrTreat.configFile.Bind(Constants.HOLLOW_GIRL, "Waiting duration", 5, "Time window during which a player can give a candy.");
        damage = TrickOrTreat.configFile.Bind(Constants.HOLLOW_GIRL, "Damage", 40, $"{Constants.HOLLOW_GIRL} damage");
        // Cursed Candy
        cursedCandyRarity = TrickOrTreat.configFile.Bind(Constants.CURSED_CANDY, "Rarity", 25, $"{Constants.CURSED_CANDY} spawn rarity.");
        minCursedCandy = TrickOrTreat.configFile.Bind(Constants.CURSED_CANDY, "Min spawn", 0, $"Min {Constants.CURSED_CANDY} to spawn");
        maxCursedCandy = TrickOrTreat.configFile.Bind(Constants.CURSED_CANDY, "Max spawn", 2, $"Max {Constants.CURSED_CANDY} to spawn");
    }
}
