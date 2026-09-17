using UnityEngine;

public class PickupItem : InteractableObject
{
    [SerializeField] private string itemName = "Item";

    public string ItemName => itemName;

    public override void Interact()
    {
        PlayerInventory inventory =
            FindFirstObjectByType<PlayerInventory>();

        if (inventory != null)
        {
            inventory.PickUp(this);
        }
    }

    // Multiplayer: interaksi membawa player pemicunya, sehingga item masuk ke
    // inventory milik player itu (GetComponent pada GO yang sama), bukan ke
    // inventory pertama yang ditemukan secara global di scene.
    public override void Interact(PlayerInteraction player)
    {
        // M2-02: networked path - item punya M2NetworkedPickup dan NGO aktif.
        // Request ke server lewat inventory pemain (agar Q tahu item mana yang
        // diminta); TIDAK ada prediksi lokal (client menunggu state server).
        if (TryGetComponent<PBJ.Playground.Abim.M2.M2NetworkedPickup>(out var networkedPickup) &&
            networkedPickup.IsSpawned)
        {
            PlayerInventory requestingInventory =
                player != null ? player.GetComponent<PlayerInventory>() : null;

            if (requestingInventory != null)
            {
                requestingInventory.RequestNetworkedPickup(networkedPickup);
            }
            else
            {
                networkedPickup.RequestPickup();
            }

            return;
        }

        PlayerInventory inventory = null;

        if (player != null)
        {
            inventory = player.GetComponent<PlayerInventory>();
        }
        else
        {
            inventory = FindFirstObjectByType<PlayerInventory>();
        }

        if (inventory != null)
        {
            inventory.PickUp(this);
        }
    }

    public void SetHeld(bool isHeld)
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = !isHeld;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = isHeld;
        }
    }
}