using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using LegaFusionCore.Managers;
using LegaFusionCore.ModsCompat;
using LethalLib.Modules;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TrickOrTreat.Behaviours;
using TrickOrTreat.Managers;
using UnityEngine;

namespace TrickOrTreat;

[BepInPlugin(modGUID, modName, modVersion)]
public class TrickOrTreat : BaseUnityPlugin
{
    internal const string modGUID = "Lega.TrickOrTreat";
    internal const string modName = "Trick Or Treat";
    internal const string modVersion = "2.0.1";

    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "trickortreat"));
    internal static ManualLogSource mls;
    public static ConfigFile configFile;

    public static GameObject cursedBallObj;
    public static GameObject cursedExplosionParticle;
    public static Item cursedCandy;

    public void Awake()
    {
        mls = BepInEx.Logging.Logger.CreateLogSource("TrickOrTreat");
        configFile = Config;
        ConfigManager.Load();

        NetcodePatcher();
        LoadItems();
        LoadEnemies();
        LoadNetworkPrefabs();
    }

    private static void NetcodePatcher()
    {
        System.Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (System.Type type in types)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                object[] attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                    _ = method.Invoke(null, null);
            }
        }
    }

    public static void LoadItems()
    {
        cursedCandy = bundle.LoadAsset<Item>("Assets/CursedCandy/CursedCandyItem.asset");
        _ = LFCObjectsManager.RegisterObject(typeof(CursedCandy), cursedCandy);
    }

    public static void LoadEnemies()
    {
        EnemyType hollowGirl = bundle.LoadAsset<EnemyType>("Assets/HollowGirl/HollowGirlEnemy.asset");
        NetworkPrefabs.RegisterNetworkPrefab(hollowGirl.enemyPrefab);
        Enemies.RegisterEnemy(hollowGirl, ConfigManager.hollowGirlRarity.Value, Levels.LevelTypes.All, null, null);
        SellBodiesFixedSoftCompat.RegisterBody(enemyName: hollowGirl.enemyName,
            item: bundle.LoadAsset<Item>("Assets/HollowGirl/HollowGirlHeadItem.asset"),
            minValue: 70,
            maxValue: 110,
            enabled: true);
    }

    public static void LoadNetworkPrefabs()
    {
        HashSet<GameObject> gameObjects =
        [
            (cursedBallObj = bundle.LoadAsset<GameObject>("Assets/CursedBall/CursedBall.prefab")),
            (cursedExplosionParticle = bundle.LoadAsset<GameObject>("Assets/CursedBall/CursedExplosionParticle.prefab"))
        ];

        foreach (GameObject gameObject in gameObjects)
        {
            NetworkPrefabs.RegisterNetworkPrefab(gameObject);
            Utilities.FixMixerGroups(gameObject);
        }
    }
}
