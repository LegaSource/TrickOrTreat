using BepInEx.Configuration;
using BepInEx;
using System.IO;
using System.Reflection;
using UnityEngine;
using LethalLib.Modules;
using BepInEx.Logging;
using System.Collections.Generic;
using TrickOrTreat.Managers;
using TrickOrTreat.Behaviours;
using CursedScraps.Values;
using HarmonyLib;
using TrickOrTreat.Patches;

namespace TrickOrTreat
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class TrickOrTreat : BaseUnityPlugin
    {
        private const string modGUID = "Lega.TrickOrTreat";
        private const string modName = "Trick Or Treat";
        private const string modVersion = "1.0.4";

        private readonly Harmony harmony = new Harmony(modGUID);
        private readonly static AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "trickortreat"));
        internal static ManualLogSource mls;
        public static ConfigFile configFile;

        public static GameObject halloweenCandySprite;

        public void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource("TrickOrTreat");
            configFile = Config;
            ConfigManager.Load();

            NetcodePatcher();
            LoadItems();
            LoadEnemies();
            LoadSprites();

            harmony.PatchAll(typeof(HUDManagerPatch));
            harmony.PatchAll(typeof(ObjectCSManagerPatch));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(BeltBagItemPatch));
            harmony.PatchAll(typeof(RoundManagerPatch));
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                        method.Invoke(null, null);
                }
            }
        }

        public static void LoadItems()
        {
            List<CustomItem> customItems = new List<CustomItem>
            {
                new CustomItem(typeof(HalloweenCandy), bundle.LoadAsset<Item>("Assets/HalloweenCandy/HalloweenCandyItem.asset"), true, ConfigManager.minHalloweenCandy.Value, ConfigManager.maxHalloweenCandy.Value, ConfigManager.halloweenCandyRarity.Value)
            };

            foreach (CustomItem customItem in customItems)
            {
                PhysicsProp script = customItem.Item.spawnPrefab.AddComponent(customItem.Type) as PhysicsProp;
                script.grabbable = true;
                script.grabbableToEnemies = true;
                script.itemProperties = customItem.Item;
                if (customItem.Item.isScrap) script.scrapValue = customItem.Value;

                NetworkPrefabs.RegisterNetworkPrefab(customItem.Item.spawnPrefab);
                Utilities.FixMixerGroups(customItem.Item.spawnPrefab);
                Items.RegisterItem(customItem.Item);
            }
            CursedScraps.CursedScraps.customItems.AddRange(customItems);
        }

        public static void LoadEnemies()
        {
            EnemyType littleGirl = bundle.LoadAsset<EnemyType>("Assets/LittleGirl/LittleGirlEnemy.asset");
            NetworkPrefabs.RegisterNetworkPrefab(littleGirl.enemyPrefab);
            Enemies.RegisterEnemy(littleGirl, ConfigManager.littleGirlRarity.Value, Levels.LevelTypes.All, null, null);
        }

        public static void LoadSprites()
            => halloweenCandySprite = bundle.LoadAsset<GameObject>("Assets/Images/HalloweenCandyImage.prefab");
    }
}
