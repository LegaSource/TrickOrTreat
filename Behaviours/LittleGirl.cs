using CursedScraps.Behaviours;
using CursedScraps.Behaviours.Curses;
using CursedScraps.Managers;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrickOrTreat.Patches;
using Unity.Netcode;
using UnityEngine;

namespace TrickOrTreat.Behaviours
{
    public class LittleGirl : EnemyAI
    {
        public Transform TurnCompass;
        public AudioClip[] FootstepSounds = Array.Empty<AudioClip>();
        public float footstepTimer = 0f;

        public bool isWaiting = false;
        public bool isBaited = false;

        public static Coroutine interactingCoroutine;
        public static Coroutine fleeingCoroutine;

        public enum State
        {
            WANDERING,
            CHASING,
            INTERACTING,
            FLEEING
        }

        public override void Start()
        {
            base.Start();

            currentBehaviourStateIndex = (int)State.WANDERING;
            creatureAnimator.SetBool("startWalk", true);
            StartSearch(transform.position);
        }

        public override void Update()
        {
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
            if (targetPlayer != null && (state == (int)State.CHASING || state == (int)State.INTERACTING))
            {
                TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
            }
        }

        public void PlayFootstepSound()
        {
            if (currentBehaviourStateIndex == (int)State.INTERACTING) return;

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
                    agent.speed = 4f;
                    if (FoundClosestPlayerInRange(25f, 10f))
                    {
                        StopSearch(currentSearch);
                        DoAnimationClientRpc("startRun");
                        SwitchToBehaviourClientRpc((int)State.CHASING);
                    }
                    break;
                case (int)State.CHASING:
                    agent.speed = 6f;
                    float distanceWithPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
                    if (!TargetClosestPlayerInAnyCase() || (distanceWithPlayer > 20 && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
                    {
                        StartSearch(transform.position);
                        DoAnimationClientRpc("startWalk");
                        SwitchToBehaviourClientRpc((int)State.WANDERING);
                        return;
                    }
                    if (distanceWithPlayer <= 5f)
                    {
                        DoAnimationClientRpc("startIdle");
                        SwitchToBehaviourServerRpc((int)State.INTERACTING);
                        return;
                    }
                    SetMovingTowardsTargetPlayer(targetPlayer);
                    break;
                case (int)State.INTERACTING:
                    agent.speed = 0f;
                    interactingCoroutine ??= StartCoroutine(InteractingWithPlayerCoroutine());
                    break;
                case (int)State.FLEEING:
                    agent.speed = 6f;
                    fleeingCoroutine ??= StartCoroutine(FleeingCoroutine());
                    break;

                default:
                    break;
            }
        }

        bool FoundClosestPlayerInRange(float range, float senseRange)
        {
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: true);
            if (targetPlayer == null)
            {
                TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: false);
                range = senseRange;
            }
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) < range;
        }

        bool TargetClosestPlayerInAnyCase()
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
            if (targetPlayer == null) return false;
            return true;
        }

        public IEnumerator InteractingWithPlayerCoroutine()
        {
            BeginInteractingClientRpc(Constants.MESSAGE_TRICK_OR_TREAT, Constants.LITTLE_GIRL, (int)targetPlayer.playerClientId);

            int timePassed = 0;
            while (!isBaited)
            {
                yield return new WaitForSeconds(1f);
                timePassed++;

                if (timePassed >= Managers.ConfigManager.waitingDuration.Value || Vector3.Distance(transform.position, targetPlayer.transform.position) > 20f)
                {
                    isWaiting = false;
                    List<CurseEffect> eligibleCurses = CurseCSManager.GetEligibleCurseEffects(StartOfRound.Instance.currentLevel.PlanetName);
                    PlayerCSBehaviour playerBehaviour = targetPlayer.GetComponent<PlayerCSBehaviour>();
                    if (playerBehaviour.activeCurses.Count > 0 && playerBehaviour.activeCurses.Count != eligibleCurses.Count)
                        eligibleCurses = eligibleCurses.Except(playerBehaviour.activeCurses).ToList();

                    DoAnimationClientRpc("startCurse");
                    ScreamClientRpc();

                    // Attendre le temps de l'animation
                    yield return null; // Attendre une frame pour s'assurer que l'animation commence
                    yield return new WaitForSeconds(creatureAnimator.GetCurrentAnimatorStateInfo(0).length);

                    if (eligibleCurses.Count > 0)
                        CursedScrapsNetworkManager.Instance.SetPlayerCurseEffectServerRpc((int)targetPlayer.playerClientId, eligibleCurses[new System.Random().Next(eligibleCurses.Count)].CurseName, true);
                    else
                        CursedScrapsNetworkManager.Instance.KillPlayerServerRpc((int)targetPlayer.playerClientId, Vector3.zero, true, (int)CauseOfDeath.Unknown);

                    break;
                }
            }

            DoAnimationClientRpc("startRun");
            SwitchToBehaviourClientRpc((int)State.FLEEING);

            yield return new WaitForSeconds(5f);

            EndInteractingClientRpc();
        }

        [ClientRpc]
        public void BeginInteractingClientRpc(string message, string sender, int playerId)
        {
            isWaiting = true;
            enemyType.canBeStunned = false;
            if (GameNetworkManager.Instance.localPlayerController == StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>())
                HUDManager.Instance.AddChatMessage(message, sender);
        }

        [ClientRpc]
        public void ScreamClientRpc()
            => creatureVoice.Play();

        [ClientRpc]
        public void EndInteractingClientRpc()
        {
            isWaiting = false;
            isBaited = false;
            enemyType.canBeStunned = true;
            interactingCoroutine = null;
        }

        public void InteractingWithEnemy()
        {
            if (isWaiting)
            {
                if (PlayerControllerBPatch.currentStackedCandies > 0)
                {
                    GiveCandyServerRpc();

                    PlayerControllerBPatch.currentStackedCandies--;
                    if (PlayerControllerBPatch.currentStackedCandies <= 0)
                        HUDManagerPatch.SetActive(false);

                    return;
                }
                HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_NO_CANDY);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void GiveCandyServerRpc()
            => GiveCandyClientRpc();

        [ClientRpc]
        public void GiveCandyClientRpc()
            => isBaited = true;

        public IEnumerator FleeingCoroutine()
        {
            Transform transform = base.transform;
            if (targetPlayer != null)
            {
                transform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, false, 0, false, 50, false);
                SetDestinationToPosition(transform ? transform.position : base.transform.position, true);
                targetPlayer = null;

                yield return new WaitForSeconds(5f);
            }

            StartSearch(transform.position);
            DoAnimationClientRpc("startWalk");
            SwitchToBehaviourClientRpc((int)State.WANDERING);
            fleeingCoroutine = null;
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string animationState)
            => creatureAnimator.SetTrigger(animationState);
    }
}
