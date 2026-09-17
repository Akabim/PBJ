using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float dropDistance = 1.5f;

    private PickupItem heldItem;

    // M2-02: LOCAL-ONLY request pointer for the networked drop path (never
    // replicated; server authority lives in M2NetworkedPickup.carrierClientId).
    // Needed because the networked path has no client-side prediction, so
    // heldItem stays null on this instance until/unless the server confirms.
    private PBJ.Playground.Abim.M2.M2NetworkedPickup requestedNetworkedPickup;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Drop();
        }
    }

    // M2-02: called by PickupItem's networked branch instead of a local pick up.
    // Records the request locally (so Q knows which item to drop), then sends the
    // request to the server. No local prediction / parenting happens here.
    public void RequestNetworkedPickup(PBJ.Playground.Abim.M2.M2NetworkedPickup networkedPickup)
    {
        if (networkedPickup == null || !networkedPickup.IsSpawned)
            return;

        requestedNetworkedPickup = networkedPickup;
        networkedPickup.RequestPickup();
    }

    public void PickUp(PickupItem item)
    {
        if (heldItem != null)
            return;

        if (holdPoint == null)
        {
            Debug.LogWarning("HoldPoint belum di-assign di PlayerInventory.");
            return;
        }

        heldItem = item;

        item.transform.SetParent(holdPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        item.SetHeld(true);
    }

    public void Drop()
    {
        // M2-02 networked path FIRST: with no client-side prediction, heldItem is
        // still null for a networked item, so the pending request must be handled
        // before the local early-out below. The server validates the carrier; a
        // non-server owner never touches the item's authoritative transform.
        if (requestedNetworkedPickup != null)
        {
            var pending = requestedNetworkedPickup;
            requestedNetworkedPickup = null;

            if (pending.IsSpawned)
            {
                pending.RequestDrop();
            }

            return;
        }

        if (heldItem == null)
            return;

        // M2-02 defensive branch: a networked item that somehow ended up in
        // heldItem must also drop through the server, not via local transforms.
        if (heldItem.TryGetComponent<PBJ.Playground.Abim.M2.M2NetworkedPickup>(out var networkedPickup) &&
            networkedPickup.IsSpawned)
        {
            networkedPickup.RequestDrop();
            return;
        }

        heldItem.transform.SetParent(null);

        Vector3 dropPosition =
            transform.position + transform.forward * dropDistance;

        heldItem.transform.position = dropPosition;

        heldItem.SetHeld(false);

        heldItem = null;
    }
}