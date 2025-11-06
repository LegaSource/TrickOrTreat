using CursedScraps.Managers;
using GameNetcodeStuff;
using LegaFusionCore.Managers;
using LegaFusionCore.Utilities;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static CursedScraps.Registries.CSCurseRegistry;

namespace TrickOrTreat.Behaviours;

public class CursedBall : NetworkBehaviour
{
    public Rigidbody rigidbody;

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void ThrowCursedBallEveryoneRpc(Vector3 targetPosition)
    {
        float speed = 35f;
        Vector3 toTarget = targetPosition - transform.position;

        // Séparation des composantes horizontales et verticales
        Vector3 horizontal = new Vector3(toTarget.x, 0, toTarget.z);
        float horizontalDistance = horizontal.magnitude;

        // Calcul de l'angle de lancement (en radians) pour créer un arc
        float angle = 45f * Mathf.Deg2Rad;
        float timeToReachTarget = horizontalDistance / (speed * Mathf.Cos(angle));

        // Calcul des vitesses initiales
        float verticalVelocity = (toTarget.y / timeToReachTarget) - (0.5f * Physics.gravity.y * timeToReachTarget);
        Vector3 horizontalVelocity = horizontal.normalized * (speed * Mathf.Cos(angle));

        // Ajout des forces pour le lancement
        rigidbody.velocity = Vector3.zero;
        rigidbody.AddForce(horizontalVelocity + (Vector3.up * verticalVelocity), ForceMode.VelocityChange);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || !LFCUtilities.IsServer) return;
        if (HandlePlayerHit(other)) Destroy(gameObject);
    }

    private bool HandlePlayerHit(Collider other)
    {
        PlayerControllerB player = other.GetComponent<PlayerControllerB>();
        if (player == null) return false;

        HitPlayerEveryoneRpc((int)player.playerClientId);
        return true;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void HitPlayerEveryoneRpc(int playerId)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        List<CurseEffectType> eligibleCurses = CursedScraps.Patches.RoundManagerPatch.GetEligibleCurses(StartOfRound.Instance.currentLevel.PlanetName);
        CurseEffectType curseType = eligibleCurses[new System.Random().Next(eligibleCurses.Count)];

        LFCGlobalManager.PlayAudio($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.hitProjectileAudio.name}", transform.position);
        CursedScrapsNetworkManager.Instance.ApplyPlayerCurseEveryoneRpc((int)player.playerClientId, curseType.Name, curseType.Duration);
    }
}
