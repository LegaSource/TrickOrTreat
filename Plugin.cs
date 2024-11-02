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
        private const string modVersion = "1.0.1";

        private readonly Harmony harmony = new Harmony(modGUID);
        private readonly static AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "trickortreat"));
        internal static ManualLogSource mls;
        public static ConfigFile configFile;

        public static List<Sprite> sprites = new List<Sprite>();

        public void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource("TrickOrTreat");
            configFile = Config;
            ConfigManager.Load();

            NetcodePatcher();
            LoadItems();
            LoadEnemies();
            LoadSprites();

            harmony.PatchAll(typeof(ObjectCSManagerPatch));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(BeltBagItemPatch));
            harmony.PatchAll(typeof(BeltBagInventoryUIPatch));
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
                    {
                        method.Invoke(null, null);
                    }
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
                var script = customItem.Item.spawnPrefab.AddComponent(customItem.Type) as PhysicsProp;
                script.grabbable = true;
                script.grabbableToEnemies = true;
                script.itemProperties = customItem.Item;

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
        {
            sprites = new List<Sprite>
            {
                bundle.LoadAsset<Sprite>("Assets/Images/HalloweenCandy1.png"),
                bundle.LoadAsset<Sprite>("Assets/Images/HalloweenCandy2.png"),
                bundle.LoadAsset<Sprite>("Assets/Images/HalloweenCandy3.png"),
                bundle.LoadAsset<Sprite>("Assets/Images/HalloweenCandy4.png"),
                bundle.LoadAsset<Sprite>("Assets/Images/HalloweenCandy5.png")
            };
        }
    }
}
