using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PBJ.Playground.Abim.M2
{
    // M2-02: server-authoritative networked pickup/drop for the M2 prototype item.
    //
    // Lives on the networked pickup item (scene-placed NetworkObject). All authority
    // is on the SERVER; the item's NetworkObject stays server-owned. Carrier identity
    // is a single replicated NetworkVariable<ulong> (Dropped sentinel = available).
    //
    // Request path (no client-side prediction, no ClientRpc for state):
    //   owner client presses E/Q -> PickupItem/PlayerInventory networked branch
    //   -> RequestPickup/RequestDrop here -> host: direct server validation;
    //      remote client: ServerRpc -> server validates -> sets carrierClientId
    //      -> NGO network parenting -> NetworkTransform (server authority)
    //      replicates the result on all clients.
    //
    // Validation (server only): sender identity via ServerRpcParams (never a
    // client-supplied id), item available (carrier == Dropped), distance <= 2
    // (same range as PlayerInteraction), one carried item per player (derived by
    // scanning FindObjectsByType<M2NetworkedPickup> - no registry/manager), item spawned.
    //
    // DISCONNECT PICKUP BEHAVIOR: TBD (no auto-drop/destroy implemented in M2-02).
    public class M2NetworkedPickup : NetworkBehaviour
    {
        private const ulong Dropped = ulong.MaxValue;

        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private float dropDistance = 1.5f;
        [SerializeField] private Vector3 holdLocalOffset = new Vector3(0f, 0f, 0.7f);

        // Server write, everyone read. Dropped = available; otherwise carried by that client.
        public NetworkVariable<ulong> CarrierClientId = new NetworkVariable<ulong>(
            Dropped, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool IsCarried => CarrierClientId.Value != Dropped;

        // ---------- request entry points (called by PickupItem / PlayerInventory) ----------

        public void RequestPickup()
        {
            if (!IsSpawned)
                return;

            if (IsServer)
            {
                ServerPickup(NetworkManager.LocalClientId);
            }
            else
            {
                PickupServerRpc();
            }
        }

        public void RequestDrop()
        {
            if (!IsSpawned)
                return;

            if (IsServer)
            {
                ServerDrop(NetworkManager.LocalClientId);
            }
            else
            {
                DropServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void PickupServerRpc(ServerRpcParams rpcParams = default)
        {
            ServerPickup(rpcParams.Receive.SenderClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void DropServerRpc(ServerRpcParams rpcParams = default)
        {
            ServerDrop(rpcParams.Receive.SenderClientId);
        }

        // ---------- server authority ----------

        private void ServerPickup(ulong senderClientId)
        {
            // Validation 5: item spawned/valid.
            if (NetworkObject == null || !NetworkObject.IsSpawned)
            {
                Debug.LogWarning($"[PBJ][M2] Pickup rejected: item not spawned ({gameObject.name})");
                return;
            }

            // Validation 1: sender must own a real player object.
            NetworkObject playerObject = GetPlayerNetworkObject(senderClientId);
            if (playerObject == null)
            {
                Debug.LogWarning($"[PBJ][M2] Pickup rejected: no player object for sender {senderClientId}");
                return;
            }

            // Validation 2: item must be available.
            if (IsCarried)
            {
                Debug.LogWarning($"[PBJ][M2] Pickup rejected: {gameObject.name} already carried by {CarrierClientId.Value}");
                return;
            }

            // Validation 3: distance (same range as PlayerInteraction).
            float distance = Vector3.Distance(transform.position, playerObject.transform.position);
            if (distance > interactionRange)
            {
                Debug.LogWarning($"[PBJ][M2] Pickup rejected: {gameObject.name} too far from sender {senderClientId} ({distance:F2} > {interactionRange})");
                return;
            }

            // Validation 4: one carried item per player (server-side scan, no registry).
            foreach (M2NetworkedPickup other in FindObjectsByType<M2NetworkedPickup>(FindObjectsSortMode.None))
            {
                if (other != this && other.IsCarried && other.CarrierClientId.Value == senderClientId)
                {
                    Debug.LogWarning($"[PBJ][M2] Pickup rejected: sender {senderClientId} already carries {other.gameObject.name}");
                    return;
                }
            }

            ApplyCarried(senderClientId, playerObject);
            Debug.Log($"[PBJ][M2] Item {gameObject.name} picked up by client {senderClientId}");
        }

        private void ApplyCarried(ulong carrierClientId, NetworkObject playerObject)
        {
            CarrierClientId.Value = carrierClientId;

            // NGO network parenting: parent to the player's NetworkObject root.
            // The item's NetworkTransform is disabled on EVERY instance (derived
            // locally from the replicated CarrierClientId - component enabled state
            // is not replicated by NGO) so the player's own NetworkTransform /
            // parent hierarchy drives the visual position on all clients without
            // the item's world-space NT fighting the parented movement.
            UpdateNetworkTransformState();

            // FIX 2 (M2-02): assign the carried local offset BEFORE TrySetParent.
            // The NGO ParentSyncMessage is generated inside TrySetParent and
            // snapshots the post-parent local position/rotation, so setting the
            // offset first is what replicates (0,0,0.7) to all clients. With
            // worldPositionStays:false the local values are preserved by SetParent.
            transform.localPosition = holdLocalOffset;
            transform.localRotation = Quaternion.identity;

            NetworkObject.TrySetParent(playerObject, false);

            SetHeldVisual(true);
        }

        private void ServerDrop(ulong senderClientId)
        {
            // Validation: only the actual carrier may drop.
            if (CarrierClientId.Value != senderClientId)
            {
                Debug.LogWarning($"[PBJ][M2] Drop rejected: client {senderClientId} is not the carrier of {gameObject.name} (carrier: {CarrierClientId.Value})");
                return;
            }

            NetworkObject playerObject = GetPlayerNetworkObject(senderClientId);
            if (playerObject == null)
            {
                Debug.LogWarning($"[PBJ][M2] Drop rejected: carrier {senderClientId} has no player object");
                return;
            }

            ApplyDropped(playerObject);
            Debug.Log($"[PBJ][M2] Item {gameObject.name} dropped by client {senderClientId}");
        }

        private void ApplyDropped(NetworkObject playerObject)
        {
            CarrierClientId.Value = Dropped;

            NetworkObject.TrySetParent((Transform)null, false);

            // Same rule as Ibnu's local PlayerInventory.Drop().
            transform.position = playerObject.transform.position + playerObject.transform.forward * dropDistance;

            // Every instance re-enables the item's own NetworkTransform from the
            // replicated Dropped state (server NT takes over from here).
            UpdateNetworkTransformState();

            SetHeldVisual(false);
        }

        // ---------- client visual sync (safety net; NGO parenting also replicates) ----------

        public override void OnNetworkSpawn()
        {
            // Late joiners / scene load: apply current authoritative visual state.
            CarrierClientId.OnValueChanged += OnCarrierChanged;
            // FIX 1 (M2-02): derive the item NetworkTransform enabled state from
            // the replicated carrier state on THIS instance (component enabled
            // state is not replicated; each instance must apply it locally).
            UpdateNetworkTransformState();
            if (IsCarried)
            {
                SyncCarriedVisual(CarrierClientId.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            CarrierClientId.OnValueChanged -= OnCarrierChanged;
        }

        private void OnCarrierChanged(ulong previous, ulong current)
        {
            // FIX 1 (M2-02): NetworkTransform enabled state follows the replicated
            // carrier state on EVERY instance (carried = disabled, dropped = enabled).
            UpdateNetworkTransformState();

            if (current == Dropped)
            {
                SetHeldVisual(false);
            }
            else
            {
                SyncCarriedVisual(current);
                SetHeldVisual(true);
            }
        }

        // FIX 1 (M2-02): single local rule, driven only by the replicated
        // CarrierClientId - no extra NetworkVariable, no RPC. Carried item: its own
        // NetworkTransform must not run (it is world-space and would pin the item
        // at the stale pre-pickup position, overriding the network parent). Dropped
        // item: NetworkTransform resumes server-authoritative transform sync.
        private void UpdateNetworkTransformState()
        {
            if (TryGetComponent<NetworkTransform>(out var itemTransform))
            {
                itemTransform.enabled = !IsCarried;
            }
        }

        private void SyncCarriedVisual(ulong carrierClientId)
        {
            // Client-side mirror of the server parent state. Actual authoritative
            // parenting comes from NGO parent synchronization (clients cannot
            // TrySetParent a server-owned object - it is authority-gated), so this
            // only keeps late-join visuals consistent if the parent already exists.
            NetworkObject playerObject = GetPlayerNetworkObject(carrierClientId);
            if (playerObject == null)
                return;

            if (transform.parent == playerObject.transform)
            {
                transform.localPosition = holdLocalOffset;
                transform.localRotation = Quaternion.identity;
            }
        }

        // ---------- helpers ----------

        private NetworkObject GetPlayerNetworkObject(ulong clientId)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject;
        }

        // Collider-only equivalent of PickupItem.SetHeld (item has no Rigidbody
        // by design in M2-02; same null-check pattern as Ibnu's code).
        private void SetHeldVisual(bool isHeld)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = !isHeld;
        }
    }
}

