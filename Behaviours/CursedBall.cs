using CursedScraps.Managers;
using GameNetcodeStuff;
using LegaFusionCore.Managers;
using LegaFusionCore.Utilities;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static CursedScraps.Registries.CSCurseRegistry;

namespace TrickOrTreat.Behaviours;

public class CursedBall : NetworkBehaviour
{
    public Rigidbody rigidbody;

    public bool deactivated = false;
    private int bouncesLeft = 1;

    private Vector3 lastVelocity;
    private readonly HashSet<ulong> cursedPlayerIds = [];

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void ThrowCursedBallEveryoneRpc(Vector3 startPosition, Vector3 direction, bool isOutside)
    {
        if (LFCUtilities.IsServer)
            bouncesLeft = Random.Range(0, 3);
        transform.position = startPosition;

        float speed = isOutside ? 45f : 30f;
        // Séparation des composantes horizontales et verticales
        Vector3 horizontal = new Vector3(direction.x, 0, direction.z);
        float horizontalDistance = horizontal.magnitude;

        // Calcul de l'angle de lancement (en radians) pour créer un arc
        float angle = 45f * Mathf.Deg2Rad;
        float timeToReachTarget = horizontalDistance / (speed * Mathf.Cos(angle));

        // Calcul des vitesses initiales
        float verticalVelocity = (direction.y / timeToReachTarget) - (0.5f * Physics.gravity.y * timeToReachTarget);
        Vector3 horizontalVelocity = horizontal.normalized * (speed * Mathf.Cos(angle));

        // Ajout des forces pour le lancement
        rigidbody.position = startPosition;
        rigidbody.velocity = Vector3.zero;
        rigidbody.AddForce(horizontalVelocity + (Vector3.up * verticalVelocity), ForceMode.VelocityChange);
    }

    private void FixedUpdate() => lastVelocity = rigidbody.velocity;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || deactivated || !LFCUtilities.IsServer) return;

        ContactPoint contactPoint = collision.GetContact(0);
        Vector3 point = contactPoint.point;
        Vector3 normal = contactPoint.normal;

        if (collision.collider.TryGetComponent(out PlayerControllerB _))
        {
            DeactivateCursedBall();
            PlayAudioEveryoneRpc();
            PlayCursedExplosionEveryoneRpc(point + (normal * 0.5f), Quaternion.LookRotation(normal));
            return;
        }

        if (bouncesLeft > 0)
        {
            bouncesLeft--;
            transform.position = point + (normal * 0.5f);
            rigidbody.velocity = Vector3.Reflect(lastVelocity, normal) * 0.85f;
            BounceEveryoneRpc(transform.position, rigidbody.velocity);
            return;
        }

        DeactivateCursedBall();
        PlayCursedExplosionEveryoneRpc(point + (normal * 0.5f), Quaternion.LookRotation(normal));
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void BounceEveryoneRpc(Vector3 position, Vector3 velocity)
    {
        if (!deactivated)
        {
            transform.position = position;
            rigidbody.velocity = velocity;
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PlayAudioEveryoneRpc()
        => LFCGlobalManager.PlayAudio($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.hitProjectileAudio.name}", transform.position);

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PlayCursedExplosionEveryoneRpc(Vector3 position, Quaternion rotation)
    {
        if (LFCUtilities.IsServer)
            _ = StartCoroutine(ApplyCurseCoroutine(position, duration: 2f));
        else
            DeactivateCursedBall();

        GameObject particleObj = Instantiate(TrickOrTreat.cursedExplosionParticle, position, rotation);
        ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();
        Destroy(particleObj, particleSystem.main.duration);

        LFCGlobalManager.PlayAudio(tag: $"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.poisonExplosionAudio.name}",
            position: position);
    }

    public IEnumerator ApplyCurseCoroutine(Vector3 position, float duration)
    {
        float timePassed = 0f;
        while (timePassed < duration)
        {
            foreach (Collider hitCollider in Physics.OverlapSphere(position, 2f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Collide))
            {
                PlayerControllerB player = hitCollider.GetComponent<PlayerControllerB>();
                if (player != null && !player.isPlayerDead && !cursedPlayerIds.Contains(player.playerClientId))
                {
                    _ = cursedPlayerIds.Add(player.playerClientId);

                    List<CurseEffectType> eligibleCurses = CursedScraps.Patches.RoundManagerPatch.GetEligibleCurses(StartOfRound.Instance.currentLevel.PlanetName);
                    CurseEffectType curseType = eligibleCurses[new System.Random().Next(eligibleCurses.Count)];
                    CursedScrapsNetworkManager.Instance.ApplyPlayerCurseEveryoneRpc((int)player.playerClientId, curseType.Name, curseType.Duration);
                }
            }

            yield return new WaitForSeconds(0.2f);
            timePassed += 0.2f;
        }
        Destroy(gameObject);
    }

    public void DeactivateCursedBall()
    {
        deactivated = true;
        cursedPlayerIds.Clear();

        Destroy(gameObject.GetComponentInChildren<ParticleSystem>());
        foreach (MeshRenderer renderer in gameObject.GetComponentsInChildren<MeshRenderer>())
            Destroy(renderer);
        foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>())
            Destroy(collider);
    }
}
