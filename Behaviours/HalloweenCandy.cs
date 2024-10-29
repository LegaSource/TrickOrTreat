using CursedScraps.Managers;
using GameNetcodeStuff;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TrickOrTreat.Behaviours
{
    public class HalloweenCandy : PhysicsProp
    {
        public int currentStackedItems = 1;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown && playerHeldBy != null)
            {
                Collider[] hitColliders = Physics.OverlapSphere(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward * 1.5f, 1f, 524288, QueryTriggerInteraction.Collide);
                foreach (var hitCollider in hitColliders)
                {
                    EnemyAI enemy = hitCollider.GetComponent<EnemyAICollisionDetect>()?.mainScript;
                    if (enemy != null && enemy is LittleGirl littleGirl && littleGirl.isWaiting)
                    {
                        GiveCandyServerRpc(littleGirl.NetworkObject);
                        break;
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void GiveCandyServerRpc(NetworkObjectReference enemyObject)
        {
            GiveCandyClientRpc(enemyObject);
        }

        [ClientRpc]
        private void GiveCandyClientRpc(NetworkObjectReference enemyObject)
        {
            if (enemyObject.TryGet(out NetworkObject networkObject))
            {
                EnemyAI enemy = networkObject.gameObject.GetComponentInChildren<EnemyAI>();
                if (enemy != null && enemy is LittleGirl littleGirl)
                {
                    littleGirl.isBaited = true;

                    if (currentStackedItems > 1)
                    {
                        currentStackedItems--;
                        UpdateIcon(playerHeldBy.currentItemSlot, currentStackedItems);
                        playerHeldBy.itemAudio.PlayOneShot(itemProperties.dropSFX);
                    }
                    else
                    {
                        DestroyObjectInHand(playerHeldBy);
                    }
                }
            }
        }

        public void UpdateIcon(int slot, int num)
        {
            HUDManager.Instance.itemSlotIcons[slot].sprite = TrickOrTreat.sprites.FirstOrDefault(s => s.name.Equals($"HalloweenCandy{num}"));
        }

        [ServerRpc(RequireOwnership = false)]
        public void DestroyObjectServerRpc()
        {
            DestroyObjectClientRpc();
        }

        [ClientRpc]
        public void DestroyObjectClientRpc()
        {
            DestroyObjectInHand(playerHeldBy);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnObjectServerRpc(int playerId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            Vector3 position = player.gameplayCamera.transform.position + player.gameplayCamera.transform.forward;
            if (Physics.Raycast(position, Vector3.down, out var hitInfo, 80f, 268437760, QueryTriggerInteraction.Ignore))
            {
                position = hitInfo.point;
            }
            GrabbableObject grabbableObject = ObjectCSManager.SpawnItem(ref itemProperties.spawnPrefab, ref position);
            SpawnObjectClientRpc(playerId, grabbableObject.GetComponent<NetworkObject>(), position);
        }

        [ClientRpc]
        public void SpawnObjectClientRpc(int playerId, NetworkObjectReference obj, Vector3 position)
        {
            if (obj.TryGet(out var networkObject))
            {
                GrabbableObject grabbableObject = networkObject.gameObject.GetComponentInChildren<GrabbableObject>();
                grabbableObject.transform.position = position + Vector3.up;
                grabbableObject.startFallingPosition = transform.position;

                PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
                if (player.isInElevator)
                {
                    grabbableObject.transform.SetParent(player.playersManager.elevatorTransform, worldPositionStays: true);
                }

                if ((bool)grabbableObject.transform.parent)
                {
                    grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(grabbableObject.startFallingPosition);
                }
                grabbableObject.FallToGround();
                if ((bool)grabbableObject.itemProperties.dropSFX)
                {
                    player.itemAudio.PlayOneShot(grabbableObject.itemProperties.dropSFX);
                }
            }
        }
    }
}
