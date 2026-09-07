using Unity.Netcode;
using UnityEngine;

namespace PBJ.Networking
{
    // M1-03: minimal networked player identity, for spawn verification only.
    // M1-04: adds IsOwner-guarded input feedback to verify input authority.
    // Technical placeholder - does NOT lock final art/gameplay direction.
    // The Space-key log is OWNERSHIP VERIFICATION ONLY, not movement (M1-05 scope).
    public class NetworkPlayerIdentity : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            gameObject.name = $"Player_{OwnerClientId}";
            Debug.Log($"[PBJ][Network][NETWORK] Player spawned for client {OwnerClientId} ({(IsOwner ? "owned by this instance" : "remote replica")})");
            if (IsOwner)
            {
                Debug.Log($"[PBJ][Network][OWNER] Local ownership confirmed for {gameObject.name} (client {OwnerClientId})");
            }
        }

        public override void OnNetworkDespawn()
        {
            Debug.Log($"[PBJ][Network][NETWORK] Player despawned for client {OwnerClientId}");
        }

        // M1-04: input authority check. Only the owning client processes local input;
        // non-owner replicas hit the IsOwner guard and ignore it. Log-only feedback:
        // proves "input source A -> Player A only" without implementing movement (M1-05).
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (!IsOwner) return; // non-owners must not react to local input

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                Debug.Log($"[PBJ][Network][OWNER] Local client {OwnerClientId} controls {gameObject.name}");
            }
#endif
        }
    }
}
