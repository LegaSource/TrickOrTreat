using Unity.Netcode;

namespace TrickOrTreat.Behaviours
{
    public class HalloweenCandy : PhysicsProp
    {
        [ServerRpc(RequireOwnership = false)]
        public void DestroyObjectServerRpc()
            => DestroyObjectClientRpc();

        [ClientRpc]
        public void DestroyObjectClientRpc()
            => DestroyObjectInHand(playerHeldBy);
    }
}
