using Unity.Netcode;
using UnityEngine;

namespace PBJ.Networking
{
    // M1-03: server-authoritative player spawning.
    // Uses the NGO ConnectionApproval callback so the SERVER decides when and where
    // a player object is spawned. Clients never spawn players themselves.
    // Ownership is the standard NGO default: the spawned player is owned by its client.
    public class PlayerSpawnHandler : MonoBehaviour
    {
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
            NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback -= ApproveConnection;
            }
        }

        // NGO invokes this callback on the server only (host included).
        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Position = spawnPoint + Vector3.right * (request.ClientNetworkId * clientSpacing);
            response.Rotation = Quaternion.identity;

            if (NetworkManager.Singleton.LocalClientId != request.ClientNetworkId)
            {
                Debug.Log($"[PBJ][Network][NETWORK] Spawning player for client {request.ClientNetworkId}");
            }
        }
    }
}
