using UnityEngine;

public class PickupItem : InteractableObject
{
    [SerializeField] private string itemName = "Item";

    private float restHeight;
    private Quaternion restRotation = Quaternion.identity;
    private bool hasRestPose;

    public string ItemName => itemName;

    public override void Interact()
    {
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();

        if (inventory != null)
            inventory.PickUp(this);
    }

    public override void Interact(GameObject interactor)
    {
        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();

        if (inventory != null)
            inventory.PickUp(this);
    }

    public override string GetPrompt()
    {
        return "Ambil " + itemName;
    }

    public void RememberRestPose()
    {
        restHeight = transform.position.y;
        restRotation = transform.rotation;
        hasRestPose = true;
    }

    public void ApplyRestPose(Vector3 position)
    {
        if (hasRestPose)
            position.y = restHeight;

        transform.position = position;
        transform.rotation = hasRestPose ? restRotation : Quaternion.identity;
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