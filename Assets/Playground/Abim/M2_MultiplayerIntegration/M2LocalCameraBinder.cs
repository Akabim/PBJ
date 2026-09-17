using Unity.Netcode;
using UnityEngine;

namespace PBJ.Playground.Abim.M2
{
    // M2 integration test: LOCAL-ONLY camera binding for multiplayer instances.
    // Ibnu's CameraFollow is a plain MonoBehaviour with a serialized target — in a
    // networked scene every instance must follow its OWN player, not a shared one.
    //
    // This binder waits for the local client's owned player to spawn (IsOwner),
    // then assigns it as the CameraFollow target. No camera transform, follow, or
    // ownership is network-synced — each instance binds independently and purely
    // locally. No network camera system is created. Offline (no NetworkManager /
    // not connected), the scene's original serialized target is simply kept.
    public class M2LocalCameraBinder : MonoBehaviour
    {
        [Header("Local Camera (CameraFollow, Ibnu)")]
        [SerializeField] private CameraFollow cameraFollow;

        private bool _bound;

        private void OnEnable()
        {
            if (cameraFollow == null)
            {
                Debug.LogError("[PBJ][M2] M2LocalCameraBinder requires a CameraFollow reference");
                enabled = false;
                return;
            }
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnClientConnected(ulong clientId) => TryBind();

        private void TryBind()
        {
            if (_bound) return;
            var nm = NetworkManager.Singleton;
            if (nm == null || nm.SpawnManager == null) return;

            foreach (var nob in nm.SpawnManager.SpawnedObjectsList)
            {
                if (nob.OwnerClientId == nm.LocalClientId && nob.IsOwner)
                {
                    Bind(nob.transform);
                    return;
                }
            }
        }

        private void LateUpdate()
        {
            if (!_bound) TryBind();
        }

        private void Bind(Transform player)
        {
            // CameraFollow.target is a private serialized field; minimal reflection
            // to avoid modifying Ibnu's script. Kept to one field, one assignment.
            var field = typeof(CameraFollow).GetField("target",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError("[PBJ][M2] CameraFollow.target field not found — camera stays on scene default");
                enabled = false;
                return;
            }
            field.SetValue(cameraFollow, player);
            _bound = true;
            Debug.Log($"[PBJ][M2] Local camera bound to {player.name} (local-only, no network sync)");
        }
    }
}
