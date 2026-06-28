using CursedScraps.Managers;
using GameNetcodeStuff;
using LegaFusionCore.Behaviours;
using LegaFusionCore.Managers;
using System.Collections.Generic;
using UnityEngine;
using static CursedScraps.Registries.CSCurseRegistry;

namespace TrickOrTreat.Behaviours;

public class CursedBall : LFCBouncyAoEProjectile
{
    protected override void PlayExplosionFx(Vector3 position, Quaternion rotation)
    {
        GameObject obj = Instantiate(TrickOrTreat.cursedExplosionParticle, position, rotation);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        Destroy(obj, ps != null ? ps.main.duration : 2f);
    }

    protected override void PlayExplosionSfx(Vector3 position)
        => LFCGlobalManager.PlayAudio($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.poisonExplosionAudio.name}", position);

    protected override void OnAffectPlayerServer(PlayerControllerB player)
    {
        List<CurseEffectType> eligible = CursedScraps.Patches.RoundManagerPatch.GetEligibleCurses(StartOfRound.Instance.currentLevel.PlanetName);
        if (eligible != null && eligible.Count > 0)
        {
            CurseEffectType curse = eligible[Random.Range(0, eligible.Count)];
            CursedScrapsNetworkManager.Instance.ApplyPlayerCurseEveryoneRpc((int)player.playerClientId, curse.Name, curse.Duration);
        }
    }

    protected override void OnAffectEnemyServer(EnemyAI enemy)
    {
        if (enemy is HollowGirl hollowGirl && !hollowGirl.isCursed)
            hollowGirl.ApplyCurseEveryoneRpc();
    }
}
