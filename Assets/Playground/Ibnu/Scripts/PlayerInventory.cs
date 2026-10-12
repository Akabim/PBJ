using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float dropDistance = 1.5f;

    private PickupItem heldItem;

    public PickupItem HeldItem => heldItem;
    public bool IsHolding => heldItem != null;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Drop();
        }
    }

    public bool PickUp(PickupItem item)
    {
        if (item == null)
            return false;

        if (heldItem != null)
        {
            GameMessage.Show("Tangan penuh. Tekan Q untuk drop.");
            return false;
        }

        if (holdPoint == null)
        {
            Debug.LogWarning("HoldPoint belum di-assign di PlayerInventory.");
            return false;
        }

        item.RememberRestPose();
        heldItem = item;

        item.transform.SetParent(holdPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        item.SetHeld(true);
        return true;
    }

    public void Drop()
    {
        if (heldItem == null)
            return;

        PickupItem item = heldItem;
        heldItem = null;

        item.transform.SetParent(null);

        Vector3 dropPosition =
            transform.position + transform.forward * dropDistance;

        item.ApplyRestPose(dropPosition);
        item.SetHeld(false);
    }

    public void DeliverHeldItem()
    {
        if (heldItem == null)
            return;

        heldItem.transform.SetParent(null);
        heldItem.gameObject.SetActive(false);
        heldItem = null;
    }
}