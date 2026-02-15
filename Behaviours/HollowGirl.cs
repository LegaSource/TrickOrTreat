using GameNetcodeStuff;
using LegaFusionCore.Behaviours.Shaders;
using LegaFusionCore.Managers;
using LegaFusionCore.Utilities;
using System;
using System.Collections;
using TrickOrTreat.Managers;
using Unity.Netcode;
using UnityEngine;
using static CursedScraps.Registries.CSCurseRegistry;

namespace TrickOrTreat.Behaviours;

public class HollowGirl : EnemyAI
{
    public Transform TurnCompass;
    public AudioClip[] FootstepSounds = Array.Empty<AudioClip>();
    public AudioClip SwingSound;
    public Transform ThrowPoint;

    public float footstepTimer = 0f;
    public float throwTimer = 0f;
    public float angerTimer = 0f;

    public float throwCooldown = 5f;
    public float angerCooldown = 30f;

    public bool canThrow = false;
    public bool isAngry = false;
    public bool isWaiting = false;
    public bool isCursed = false;

    public Coroutine stunCoroutine;
    public Coroutine throwCoroutine;
    public Coroutine interactingCoroutine;
    public Coroutine swingCoroutine;

    public enum State { WANDERING, CHASING, THROWING, INTERACTING }

    public override void Start()
    {
        base.Start();

        currentBehaviourStateIndex = (int)State.WANDERING;
        StartSearch(transform.position);
        if (LFCUtilities.IsServer)
        {
            for (int i = 0; i < ConfigManager.maxCursedCandy.Value; i++)
            {
                if (i < ConfigManager.minCursedCandy.Value || UnityEngine.Random.Range(1, 101) <= ConfigManager.cursedCandyRarity.Value)
                    LFCObjectsManager.SpawnNewObject(RoundManager.Instance, TrickOrTreat.cursedCandy);
            }
        }
    }

    public override void Update()
    {
        base.Update();

        PlayFootstepSound();
        int state = currentBehaviourStateIndex;
        if (targetPlayer != null && (state == (int)State.CHASING || state == (int)State.INTERACTING || state == (int)State.THROWING))
        {
            TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
        }
        LFCUtilities.UpdateTimer(ref angerTimer, angerCooldown, isAngry, () => isAngry = false);
        LFCUtilities.UpdateTimer(ref throwTimer, throwCooldown, !canThrow, () => canThrow = true);
    }

    public void PlayFootstepSound()
    {
        AnimatorClipInfo[] info = creatureAnimator.GetCurrentAnimatorClipInfo(0);
        if (info.Length != 0 && (info[0].clip.name.Contains("walk") || info[0].clip.name.Contains("run")))
        {
            footstepTimer -= Time.deltaTime;
            if (FootstepSounds.Length > 0 && footstepTimer <= 0)
            {
                creatureSFX.PlayOneShot(FootstepSounds[UnityEngine.Random.Range(0, FootstepSounds.Length)]);
                footstepTimer = info[0].clip.name.Contains("walk") ? 0.6f : 0.4f;
            }
        }
    }

    public override void SetEnemyStunned(bool setToStunned, float setToStunTime = 1.917f, PlayerControllerB setStunnedByPlayer = null)
    {
        if (LFCUtilities.IsServer && setToStunned && stunCoroutine == null)
        {
            base.SetEnemyStunned(setToStunned, setToStunTime, setStunnedByPlayer);
            stunCoroutine = StartCoroutine(StunCoroutine());
        }
    }

    public IEnumerator StunCoroutine()
    {
        CancelThrowCoroutine();
        CancelInteractingCoroutine();
        CancelSwingCoroutine();

        agent.speed = 0f;
        DoAnimationEveryoneRpc("startStun");
        yield return this.WaitForFullAnimation("stun");

        while (stunNormalizedTimer > 0f)
            yield return null;

        while (postStunInvincibilityTimer > 0f)
            yield return null;

        DoAnimationEveryoneRpc("startRun");
        if (currentBehaviourStateIndex != (int)State.CHASING && stunnedByPlayer != null)
        {
            targetPlayer = stunnedByPlayer;
            StopSearch(currentSearch);
            SwitchToBehaviourClientRpc((int)State.CHASING);
        }

        stunCoroutine = null;
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.WANDERING: DoWandering(); break;
            case (int)State.CHASING: DoChasing(); break;
            case (int)State.THROWING: DoThrowing(); break;
            case (int)State.INTERACTING: DoInteracting(); break;
        }
    }

    public void DoWandering()
    {
        agent.speed = 3f;
        if (this.FoundClosestPlayerInRange(25, 10))
        {
            StopSearch(currentSearch);
            DoAnimationEveryoneRpc("startRun");
            SwitchToBehaviourClientRpc((int)State.CHASING);
        }
    }

    public void DoChasing()
    {
        if (swingCoroutine != null) return;

        agent.speed = 6f;
        if (this.TargetOutsideChasedPlayer()) return;
        if (!this.TargetClosestPlayerInAnyCase(out float distanceWithPlayer) || (distanceWithPlayer > 30f && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
        {
            StartSearch(transform.position);
            DoAnimationEveryoneRpc("startWalk");
            SwitchToBehaviourClientRpc((int)State.WANDERING);
            return;
        }
        if (HoldingCursedCandy(targetPlayer))
        {
            if (distanceWithPlayer <= 5f)
            {
                agent.speed = 0f;
                DoAnimationEveryoneRpc("startIdle");
                SwitchToBehaviourClientRpc((int)State.INTERACTING);
                return;
            }
        }
        else if (!CanHitPlayer(targetPlayer) && distanceWithPlayer <= 15f && (distanceWithPlayer <= 2f || CheckLineOfSightForPosition(targetPlayer.transform.position)))
        {
            SwitchToBehaviourClientRpc((int)State.THROWING);
            return;
        }
        SetMovingTowardsTargetPlayer(targetPlayer);
    }

    public void DoThrowing()
    {
        if (throwCoroutine != null) return;

        agent.speed = 6f;
        float distanceWithPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (HoldingCursedCandy(targetPlayer))
        {
            if (distanceWithPlayer <= 5f)
            {
                DoAnimationEveryoneRpc("startIdle");
                SwitchToBehaviourClientRpc((int)State.INTERACTING);
                return;
            }
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
        if (CanHitPlayer(targetPlayer) || distanceWithPlayer > 20f || (distanceWithPlayer > 2f && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
        {
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
        if (canThrow)
        {
            canThrow = false;
            throwCoroutine = StartCoroutine(ThrowCoroutine());
            return;
        }
        SetMovingTowardsTargetPlayer(targetPlayer);
    }

    public IEnumerator ThrowCoroutine()
    {
        agent.speed = 0f;
        DoAnimationEveryoneRpc("startIdle");
        yield return this.WaitForFullAnimation("idle");

        DoAnimationEveryoneRpc("startThrow");
        yield return new WaitForSeconds(0.13f);
        PlayThrowEveryoneRpc();

        GameObject gameObject = Instantiate(TrickOrTreat.cursedBallObj, ThrowPoint.transform.position, Quaternion.identity);
        gameObject.GetComponent<NetworkObject>().Spawn();
        gameObject.GetComponent<CursedBall>().ThrowCursedBallEveryoneRpc(startPosition: ThrowPoint.transform.position,
            direction: targetPlayer.transform.position + (Vector3.up * 1.5f) - ThrowPoint.transform.position,
            isOutside: isOutside);

        yield return this.WaitForFullAnimation("throw");
        DoAnimationEveryoneRpc("startRun");

        throwCoroutine = null;
    }

    public void CancelThrowCoroutine()
    {
        if (throwCoroutine != null)
        {
            StopCoroutine(throwCoroutine);
            throwCoroutine = null;
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PlayThrowEveryoneRpc() => creatureVoice.Play();

    public void DoInteracting()
    {
        agent.speed = 0f;
        interactingCoroutine ??= StartCoroutine(InteractingCoroutine());
    }

    public IEnumerator InteractingCoroutine()
    {
        StartInteractingEveryoneRpc((int)targetPlayer.playerClientId);

        int timePassed = 0;
        while (timePassed < ConfigManager.waitingDuration.Value && Vector3.Distance(transform.position, targetPlayer.transform.position) <= 20f)
        {
            yield return new WaitForSeconds(1f);
            timePassed++;

            if (isCursed) break;
        }

        CancelInteractingCoroutine();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void StartInteractingEveryoneRpc(int playerId)
    {
        isWaiting = true;
        enemyType.canBeStunned = false;

        if (LFCUtilities.ShouldBeLocalPlayer(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>()))
            HUDManager.Instance.AddChatMessage(Constants.MESSAGE_TRICK_OR_TREAT, Constants.HOLLOW_GIRL);
    }

    public void CancelInteractingCoroutine()
    {
        if (interactingCoroutine != null)
        {
            StopCoroutine(interactingCoroutine);
            interactingCoroutine = null;
            EndInteractingEveryoneRpc();
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void EndInteractingEveryoneRpc()
    {
        creatureAnimator.SetTrigger("startRun");
        SwitchToBehaviourStateOnLocalClient((int)State.CHASING);

        isAngry = true;
        isWaiting = false;
        enemyType.canBeStunned = true;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void ApplyCurseEveryoneRpc()
    {
        isCursed = true;
        CustomPassManager.SetupAuraForObjects([gameObject], CursedScraps.CursedScraps.cursedShader, $"{TrickOrTreat.modName}{CursedScraps.CursedScraps.cursedShader.name}");
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);

        if (currentBehaviourStateIndex != (int)State.CHASING || swingCoroutine != null) return;
        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (!LFCUtilities.ShouldBeLocalPlayer(player) || !CanHitPlayer(player)) return;

        SwingEveryoneRpc((int)player.playerClientId);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SwingEveryoneRpc(int playerId) => swingCoroutine ??= StartCoroutine(SwingCoroutine(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>()));

    public IEnumerator SwingCoroutine(PlayerControllerB player)
    {
        agent.speed = 0f;
        creatureAnimator.SetTrigger("startSwing");
        creatureSFX.PlayOneShot(SwingSound);
        player.DamagePlayer(ConfigManager.damage.Value, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing);

        yield return this.WaitForFullAnimation("swing");

        creatureAnimator.SetTrigger("startRun");
        agent.speed = 6f;
        swingCoroutine = null;
    }

    public void CancelSwingCoroutine()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (!isEnemyDead && CanHitPlayer(playerWhoHit))
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            enemyHP -= force;
            if (enemyHP <= 0 && IsOwner) KillEnemyOnOwnerClient();
        }
    }

    public bool CanHitPlayer(PlayerControllerB player) => isCursed || (player != null && HasCurse(player.gameObject));
    public bool HoldingCursedCandy(PlayerControllerB player) => !isCursed && !isAngry && player.currentlyHeldObjectServer != null && player.currentlyHeldObjectServer is CursedCandy;

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DoAnimationEveryoneRpc(string animationState) => creatureAnimator.SetTrigger(animationState);
}
