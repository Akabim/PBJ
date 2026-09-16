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