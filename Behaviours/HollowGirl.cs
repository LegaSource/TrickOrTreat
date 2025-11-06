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
    public AudioClip DieSound;

    public float footstepTimer = 0f;
    public float throwTimer = 0f;
    public float angerTimer = 0f;

    public static float throwCooldown = 1f;
    public static float angerCooldown = 30f;

    public bool isAngry = false;
    public bool isWaiting = false;
    public bool isCursed = false;

    public Coroutine interactingCoroutine;
    public Coroutine attackCoroutine;
    public Coroutine killCoroutine;

    public enum State
    {
        WANDERING,
        CHASING,
        THROWING,
        INTERACTING
    }

    public override void Start()
    {
        base.Start();

        currentBehaviourStateIndex = (int)State.WANDERING;
        creatureAnimator.SetTrigger("startWalk");
        StartSearch(transform.position);

        if (LFCUtilities.IsServer)
        {
            for (int i = 0; i < ConfigManager.maxCursedCandy.Value; i++)
            {
                if (i < ConfigManager.minCursedCandy.Value || new System.Random().Next(1, 100) <= ConfigManager.cursedCandyRarity.Value)
                    LFCObjectsManager.SpawnNewObject(RoundManager.Instance, TrickOrTreat.cursedCandy);
            }
        }
    }

    public override void Update()
    {
        if (killCoroutine != null) return;
        base.Update();

        creatureAnimator.SetBool("stunned", stunNormalizedTimer > 0f);
        if (stunNormalizedTimer > 0f)
        {
            agent.speed = 0f;
            if (stunnedByPlayer != null)
            {
                targetPlayer = stunnedByPlayer;
                StopSearch(currentSearch);
                SwitchToBehaviourClientRpc((int)State.CHASING);
            }
            return;
        }
        PlayFootstepSound();
        int state = currentBehaviourStateIndex;
        if (targetPlayer != null && (state == (int)State.CHASING || state == (int)State.INTERACTING || state == (int)State.THROWING))
        {
            TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);

            if (state == (int)State.THROWING) throwTimer += Time.deltaTime;
        }
        if (isAngry)
        {
            angerTimer += Time.deltaTime;
            if (angerTimer >= angerCooldown)
            {
                isAngry = false;
                angerTimer = 0f;
            }
        }
    }

    public void PlayFootstepSound()
    {
        if (currentBehaviourStateIndex == (int)State.INTERACTING || currentBehaviourStateIndex == (int)State.THROWING || attackCoroutine != null) return;

        footstepTimer -= Time.deltaTime;
        if (FootstepSounds.Length > 0 && footstepTimer <= 0)
        {
            creatureSFX.PlayOneShot(FootstepSounds[UnityEngine.Random.Range(0, FootstepSounds.Length)]);
            footstepTimer = 0.6f;
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.WANDERING:
                DoWandering();
                break;
            case (int)State.CHASING:
                DoChasing();
                break;
            case (int)State.THROWING:
                DoThrowing();
                break;
            case (int)State.INTERACTING:
                DoInteracting();
                break;
        }
    }

    public void DoWandering()
    {
        agent.speed = 3f;
        if (FoundClosestPlayerInRange(25, 10))
        {
            StopSearch(currentSearch);
            DoAnimationEveryoneRpc("startChase");
            SwitchToBehaviourClientRpc((int)State.CHASING);
        }
    }

    private bool FoundClosestPlayerInRange(int range, int senseRange)
    {
        PlayerControllerB player = CheckLineOfSightForPlayer(60f, range, senseRange);
        return player != null && PlayerIsTargetable(player) && (bool)(targetPlayer = player);
    }

    public void DoChasing()
    {
        agent.speed = 6f;
        throwCooldown = 1f;
        float distanceWithPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (!TargetClosestPlayerInAnyCase() || (distanceWithPlayer > 30 && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
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
                DoAnimationEveryoneRpc("startIdle");
                SwitchToBehaviourServerRpc((int)State.INTERACTING);
                return;
            }
        }
        else if (CanThrow(distanceWithPlayer))
        {
            DoAnimationEveryoneRpc("startIdle");
            SwitchToBehaviourServerRpc((int)State.THROWING);
            return;
        }
        SetMovingTowardsTargetPlayer(targetPlayer);
    }

    public bool TargetClosestPlayerInAnyCase()
    {
        mostOptimalDistance = 2000f;
        targetPlayer = null;
        for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
        {
            tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
            if (tempDist < mostOptimalDistance)
            {
                mostOptimalDistance = tempDist;
                targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
            }
        }
        return targetPlayer != null;
    }

    public void DoThrowing()
    {
        agent.speed = 0f;
        float distanceWithPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (HoldingCursedCandy(targetPlayer))
        {
            throwTimer = 0f;
            if (distanceWithPlayer <= 5f)
            {
                DoAnimationEveryoneRpc("startIdle");
                SwitchToBehaviourServerRpc((int)State.INTERACTING);
                return;
            }
            DoAnimationEveryoneRpc("startChase");
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
        if (!CanThrow(distanceWithPlayer))
        {
            throwTimer = 0f;
            DoAnimationEveryoneRpc("startChase");
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
        ThrowCursedBall(targetPlayer);
    }

    public void ThrowCursedBall(PlayerControllerB player)
    {
        if (throwTimer < throwCooldown) return;

        DoAnimationEveryoneRpc("startThrow");
        PlayThrowEveryoneRpc();

        GameObject gameObject = Instantiate(TrickOrTreat.cursedBallObj, transform.position + (Vector3.up * 1.5f), Quaternion.identity, StartOfRound.Instance.propsContainer);
        CursedBall cursedBall = gameObject.GetComponent<CursedBall>();
        gameObject.GetComponent<NetworkObject>().Spawn();
        cursedBall.ThrowCursedBallEveryoneRpc(player.transform.position + (Vector3.up * 1.5f));

        DoAnimationEveryoneRpc("startIdle");
        throwTimer = 0f;
        throwCooldown = 5f;
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

        EndInteractingEveryoneRpc();
        interactingCoroutine = null;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void StartInteractingEveryoneRpc(int playerId)
    {
        isWaiting = true;
        enemyType.canBeStunned = false;

        if (LFCUtilities.ShouldBeLocalPlayer(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>()))
            HUDManager.Instance.AddChatMessage(Constants.MESSAGE_TRICK_OR_TREAT, Constants.HOLLOW_GIRL);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void EndInteractingEveryoneRpc()
    {
        creatureAnimator.SetTrigger("startChase");
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

        if (currentBehaviourStateIndex != (int)State.CHASING || attackCoroutine != null || killCoroutine != null) return;
        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (!LFCUtilities.ShouldBeLocalPlayer(player) || !CanHitPlayer(player)) return;

        AttackEveryoneRpc((int)player.playerClientId);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void AttackEveryoneRpc(int playerId) => attackCoroutine ??= StartCoroutine(AttackCoroutine(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>()));

    public IEnumerator AttackCoroutine(PlayerControllerB player)
    {
        agent.speed = 0f;
        creatureAnimator.SetTrigger("startAttack");
        creatureAnimator.SetTrigger("startChase");
        creatureSFX.PlayOneShot(SwingSound);

        yield return new WaitForSeconds(1.1f);

        player.DamagePlayer(ConfigManager.damage.Value, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing);
        creatureAnimator.SetTrigger("startChase");
        agent.speed = 6f;
        attackCoroutine = null;
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (isEnemyDead || !CanHitPlayer(playerWhoHit)) return;
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

        enemyHP -= force;
        if (enemyHP <= 0 && IsOwner) KillEnemyOnOwnerClient();
    }

    public override void KillEnemy(bool destroy = false) => killCoroutine = StartCoroutine(KillEnemyCoroutine(destroy));

    public IEnumerator KillEnemyCoroutine(bool destroy)
    {
        creatureAnimator.SetTrigger("startKill");
        creatureSFX.PlayOneShot(DieSound);

        yield return new WaitForSeconds(3f);

        LFCGlobalManager.PlayParticle($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.darkExplosionParticle.name}", transform.position, Quaternion.Euler(-90, 0, 0));
        LFCGlobalManager.PlayAudio($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.darkExplosionAudio.name}", transform.position);
        base.KillEnemy(destroy);
    }

    public bool CanThrow(float distanceWithPlayer) => !CanHitPlayer(targetPlayer) && distanceWithPlayer <= 20f && (distanceWithPlayer <= 2f || CheckLineOfSightForPosition(targetPlayer.transform.position));
    public bool CanHitPlayer(PlayerControllerB player) => isCursed || (player != null && HasCurse(player.gameObject));
    public bool HoldingCursedCandy(PlayerControllerB player) => !isCursed && !isAngry && player.currentlyHeldObjectServer != null && player.currentlyHeldObjectServer is CursedCandy;

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DoAnimationEveryoneRpc(string animationState) => creatureAnimator.SetTrigger(animationState);
}
