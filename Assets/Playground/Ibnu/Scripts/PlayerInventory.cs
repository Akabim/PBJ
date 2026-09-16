using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float dropDistance = 1.5f;

    private PickupItem heldItem;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Drop();
        }
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
        if (heldItem == null)
            return;

        heldItem.transform.SetParent(null);

        Vector3 dropPosition =
            transform.position + transform.forward * dropDistance;

        heldItem.transform.position = dropPosition;

        heldItem.SetHeld(false);

        heldItem = null;
    }
}