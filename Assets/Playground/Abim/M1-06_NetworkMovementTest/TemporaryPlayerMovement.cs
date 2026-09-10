using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PBJ.Playground.Abim.M106Test
{
    // M1-06 TEST PROTOTYPE - TEMPORARY. NOT FINAL MOVEMENT.
    // Purpose: prove that position changes on the local owner flow to all other
    // instances through the existing NetworkTransform (built-in NGO component).
    // This will be REPLACED by the M1-05 movement work (Ibnu). Do not build on it.
    //
    // Design (kept as simple as possible on purpose):
    // - Local input driver only. No gravity, collision, acceleration, sprint, animation.
    // - 8-directional WASD in the XZ plane, diagonal input normalized via ClampMagnitude(1).
    // - Uses the Input System package (project activeInputHandler = Input System).
    // - Authority/ownership model is NOT changed here: no new RPCs, no new NetworkVariables.
    //   Sync is entirely owned by the NetworkTransform on this same GameObject.
    // - Reads the active NetworkTransform.AuthorityMode and only moves when this
    //   instance is the actual transform authority, so local driving stays correct
    //   for either NetworkTransform configuration.
    [DisallowMultipleComponent]
    public class TemporaryPlayerMovement : NetworkBehaviour
    {
        [Header("Temporary M1-06 Test Movement")]
        [SerializeField] private float moveSpeed = 5f;

        private NetworkTransform _networkTransform;

        // True when THIS machine is the instance that pushes transform state updates.
        // Server authority mode: server/host drives. Owner authority mode: owning client drives.
        private bool IsTransformAuthority =>
            _networkTransform != null &&
            (_networkTransform.AuthorityMode == NetworkTransform.AuthorityModes.Owner
                ? IsOwner
                : IsServer);

        public override void OnNetworkSpawn()
        {
            _networkTransform = GetComponent<NetworkTransform>();
            if (_networkTransform == null)
            {
                Debug.LogError($"[{nameof(TemporaryPlayerMovement)}] {gameObject.name} has no NetworkTransform. " +
                               "This prototype requires one for sync; input driving is disabled.");
            }
        }

        private void Update()
        {
            // Remote replicas must never react to local input. Ownership check first:
            // one guard covers both "not mine" and "not the transform authority".
            if (!IsTransformAuthority) return;

#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            float x = 0f;
            float z = 0f;
            if (keyboard.wKey.isPressed) z += 1f;
            if (keyboard.sKey.isPressed) z -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.aKey.isPressed) x -= 1f;

            if (x == 0f && z == 0f) return;

            // Normalize diagonals (8-direction, constant speed).
            var move = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
            transform.position += move * (moveSpeed * Time.deltaTime);
#endif
        }
    }
}
