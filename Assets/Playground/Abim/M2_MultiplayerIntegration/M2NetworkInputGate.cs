using Unity.Netcode;
using UnityEngine;

namespace PBJ.Playground.Abim.M2
{
    // M2 integration test: MINIMAL ownership gate for Ibnu's gameplay components.
    // Ibnu's scripts (PlayerMovement, PlayerFacing, PlayerInteraction, PlayerRespawn, PlayerInventory)
    // are plain MonoBehaviours that always read local keyboard input. On remote
    // replicas they must not run, otherwise every instance moves every player and
    // NetworkTransform fights the motion (jitter).
    //
    // Gate approach (minimum, reversible): this NetworkBehaviour disables the
    // gameplay components while the object is NOT owned by this instance, and
    // re-enables them when it is. Ibnu's scripts are NOT modified. Removing this
    // component fully restores original single-player behaviour.
    //
    // PlayerInventory note: gated like the rest (Q = local Drop). Its serialized holdPoint on
    // the M2 test prefab is a prefab-local child (test-only config); holdPoint == null would
    // make PickUp() a safe no-op, so a null never breaks the replica.
    //
    // NetworkTransform stays Owner-authority (proven in M1-06): owner drives
    // position (via Rigidbody.MovePosition) and rotation (via PlayerFacing);
    // replicas receive both through NetworkTransform.
    public class M2NetworkInputGate : NetworkBehaviour
    {
        [Header("Gameplay Components to Gate (owner-only)")]
        [SerializeField] private Behaviour[] gameplayComponents;

        [Header("Rigidbody Handling")]
        [Tooltip("NetworkRigidbody makes replicas kinematic automatically. Keep owner gravity on so the player rests on the ground.")]
        [SerializeField] private Rigidbody ownerRigidbody;

        public override void OnNetworkSpawn()
        {
            bool owner = IsOwner;

            if (gameplayComponents != null)
            {
                foreach (var component in gameplayComponents)
                {
                    if (component != null) component.enabled = owner;
                }
            }

            if (ownerRigidbody != null)
            {
                // Owner keeps Ibnu's original settings (gravity on, freeze rotation).
                // Replicas are kinematic (NetworkRigidbody also enforces this).
                ownerRigidbody.isKinematic = !owner;
            }

            Debug.Log($"[PBJ][M2] Input gate: {(owner ? "OWNER" : "REPLICA")} for {gameObject.name} (client {OwnerClientId})");
        }

        public override void OnNetworkDespawn()
        {
            // Restore so a returned-to-pool object behaves like the original prefab.
            if (gameplayComponents != null)
            {
                foreach (var component in gameplayComponents)
                {
                    if (component != null) component.enabled = true;
                }
            }
        }
    }
}
