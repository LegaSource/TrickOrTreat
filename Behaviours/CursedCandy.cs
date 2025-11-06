using GameNetcodeStuff;
using LegaFusionCore.Behaviours.Shaders;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace TrickOrTreat.Behaviours;

public class CursedCandy : PhysicsProp
{
    public HollowGirl aimedEnemy;

    public override void Update()
    {
        base.Update();
        if (!isHeld || isPocketed || playerHeldBy == null || !LFCUtilities.ShouldBeLocalPlayer(playerHeldBy)) return;

        ShowAuraAimedEnemy(playerHeldBy);
    }

    public void ShowAuraAimedEnemy(PlayerControllerB player)
    {
        if (Physics.Raycast(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward, out RaycastHit hit, 5f, 524288, QueryTriggerInteraction.Collide)
            && hit.collider.TryGetComponent(out EnemyAICollisionDetect collision)
            && collision.mainScript is HollowGirl hollowGirl
            && hollowGirl.isWaiting)
        {
            if (aimedEnemy != hollowGirl)
            {
                RemoveAuraFromEnemy();
                aimedEnemy = hollowGirl;
                CustomPassManager.SetupAuraForObjects([hollowGirl.gameObject], LegaFusionCore.LegaFusionCore.transparentShader, $"{TrickOrTreat.modName}{gameObject.name}", Color.red);
            }
            return;
        }
        RemoveAuraFromEnemy();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!buttonDown || playerHeldBy == null || aimedEnemy == null || !aimedEnemy.isWaiting) return;

        aimedEnemy.ApplyCurseEveryoneRpc();
        LFCNetworkManager.Instance.DestroyObjectEveryoneRpc(GetComponent<NetworkObject>());
    }

    public override void PocketItem()
    {
        base.PocketItem();
        RemoveAuraFromEnemy();
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        RemoveAuraFromEnemy();
    }

    public void RemoveAuraFromEnemy()
    {
        if (aimedEnemy == null) return;

        CustomPassManager.RemoveAuraFromObjects([aimedEnemy.gameObject], $"{TrickOrTreat.modName}{gameObject.name}");
        aimedEnemy = null;
    }
}
