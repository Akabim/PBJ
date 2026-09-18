using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace PBJ.Networking
{
    // M1-03: server-authoritative player spawning.
    // Uses the NGO ConnectionApproval callback so the SERVER decides when and where
    // a player object is spawned. Clients never spawn players themselves.
    // Ownership is the standard NGO default: the spawned player is owned by its client.
    // M3: the connection approval payload now carries the client's display name.
    // The server sanitizes it with PlayerNameValidator (never trusts raw client
    // strings) and registers it for NetworkPlayerIdentity to write at spawn.
    public class PlayerSpawnHandler : MonoBehaviour
    {
        // M3: generous byte cap for the approval payload (a raw display name).
        // The real length/format rule lives in PlayerNameValidator (server-side).
        private const int MaxConnectionPayloadBytes = 128;

        // M3: sanitized display names approved per client. Consumed by
        // NetworkPlayerIdentity on spawn (server side), purged on disconnect.
        private static readonly Dictionary<ulong, string> ApprovedDisplayNames = new Dictionary<ulong, string>();

        [Header("Initial Spawn Point (deterministic, M1-03 minimum)")]
        [SerializeField] private Vector3 spawnPoint = new Vector3(0f, 1f, 0f);
        [SerializeField] private float clientSpacing = 2f;

        // Subscribe in Start (not OnEnable): Start runs after all Awake calls,
        // guaranteeing NetworkManager.Singleton is initialized. Avoids race on scene load.
        private void Start()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[PBJ][Network][NETWORK] PlayerSpawnHandler requires a NetworkManager in the same scene");
                return;
            }
            ApprovedDisplayNames.Clear(); // M3: reset registry (safe even with disabled domain reload)
            NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback -= ApproveConnection;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            ApprovedDisplayNames.Clear();
        }

        // M3 (client side): queue the player-entered display name into the NGO
        // connection approval payload. Call BEFORE StartHost()/StartClient()
        // (natural callers: NetworkSession / future name UI). This only fills
        // NetworkConfig.ConnectionData - the standard NGO approval-data channel -
        // so connection transport behavior is untouched. The server sanitizes
        // whatever arrives; raw text is never trusted.
        public static void SetConnectionPayload(string displayName)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[PBJ][Network][NETWORK] SetConnectionPayload requires a NetworkManager");
                return;
            }
            byte[] payload = string.IsNullOrEmpty(displayName)
                ? new byte[0]
                : Encoding.UTF8.GetBytes(displayName);
            if (payload.Length > MaxConnectionPayloadBytes)
            {
                Debug.LogWarning($"[PBJ][Network][NETWORK] Connection payload truncated to {MaxConnectionPayloadBytes} bytes");
                Array.Resize(ref payload, MaxConnectionPayloadBytes);
            }
            NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
        }

        // M3: server-side lookup for NetworkPlayerIdentity at spawn time.
        public static bool TryGetApprovedDisplayName(ulong clientId, out string displayName)
        {
            return ApprovedDisplayNames.TryGetValue(clientId, out displayName);
        }

        // NGO invokes this callback on the server only (host included).
        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // M3: sanitize the received name server-side BEFORE using it.
            // Empty/invalid payloads fall back to Player#### via the validator.
            string displayName = PlayerNameValidator.Sanitize(DecodePayload(request.Payload), request.ClientNetworkId);
            ApprovedDisplayNames[request.ClientNetworkId] = displayName;

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Position = spawnPoint + Vector3.right * (request.ClientNetworkId * clientSpacing);
            response.Rotation = Quaternion.identity;

            if (NetworkManager.Singleton.LocalClientId != request.ClientNetworkId)
            {
                Debug.Log($"[PBJ][Network][NETWORK] Spawning player for client {request.ClientNetworkId}");
            }
        }

        // M3: the approval payload is the raw UTF-8 display name (may be absent).
        private static string DecodePayload(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(payload);
        }

        // M3: purge the registry entry when a client leaves (server side too).
        private void OnClientDisconnected(ulong clientId)
        {
            ApprovedDisplayNames.Remove(clientId);
        }
    }
}
