using Unity.Netcode;
using UnityEngine;

namespace PBJ.Networking
{
    // M1-03: minimal networked player identity, for spawn verification only.
    // Technical placeholder - does NOT lock final art/gameplay direction.
    public class NetworkPlayerIdentity : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            gameObject.name = $"Player_{OwnerClientId}";
            Debug.Log($"[PBJ][Network][NETWORK] Player spawned for client {OwnerClientId} ({(IsOwner ? "owned by this instance" : "remote replica")})");
        }

        public override void OnNetworkDespawn()
        {
            Debug.Log($"[PBJ][Network][NETWORK] Player despawned for client {OwnerClientId}");
        }
    }
}
