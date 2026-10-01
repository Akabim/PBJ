using UnityEngine;

public class DeadBody : PickupItem
{
    public override void Interact()
    {
        PlayerInventory closest = null;
        float bestDistance = float.MaxValue;

        foreach (PlayerInventory inventory in FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None))
        {
            if (!inventory.enabled)
                continue;

            float distance = Vector3.Distance(inventory.transform.position, transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = inventory;
            }
        }

        if (closest != null)
            closest.PickUp(this);
    }
}