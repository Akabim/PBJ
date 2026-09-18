using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PBJ.Networking
{
    // M1-03: minimal networked player identity, for spawn verification only.
    // M1-04: adds IsOwner-guarded input feedback to verify input authority.
    // M3: adds the server-authoritative display name. The server sanitizes the
    // name received in the client's connection approval payload (see
    // PlayerSpawnHandler) via PlayerNameValidator and writes it into the
    // NetworkVariable below; every client can read it. Cosmetic only - NOT a
    // separate identity system, and the Space-key log stays ownership-only.
    public class NetworkPlayerIdentity : NetworkBehaviour
    {
        // M3: server-write / everyone-read display name. Filled once on the
        // server at spawn from the sanitized connection approval payload
        // (or the PlayerNameValidator fallback). Clients never write it.
        public NetworkVariable<FixedString64Bytes> DisplayName = new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // M3: convenience accessor for future name UI (M3+ scope).
        public string DisplayNameText => DisplayName.Value.ToString();

        public override void OnNetworkSpawn()
        {
            // M1-03 object naming preserved: deterministic, clientId-based.
            gameObject.name = $"Player_{OwnerClientId}";
            if (IsServer)
            {
                // M3: server assigns the sanitized display name before it
                // replicates. Never trusts raw client strings - PlayerSpawnHandler
                // already sanitized; fall back if no approved name is registered.
                if (!PlayerSpawnHandler.TryGetApprovedDisplayName(OwnerClientId, out string approvedName))
                {
                    approvedName = PlayerNameValidator.GetFallbackName(OwnerClientId);
                }
                DisplayName.Value = approvedName;
                Debug.Log($"[PBJ][Network][NETWORK] Display name '{approvedName}' assigned to {gameObject.name} (client {OwnerClientId})");
            }
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
