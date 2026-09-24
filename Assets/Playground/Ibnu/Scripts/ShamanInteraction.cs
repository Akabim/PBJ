using UnityEngine;

public class ShamanInteraction : InteractableObject
{
    [SerializeField] private GameObject objectivePanel;

    private ObjectiveManager objective;

    private void Start()
    {
        objective = FindFirstObjectByType<ObjectiveManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        PickupItem item = other.GetComponent<PickupItem>();

        if (item == null || objective == null)
            return;

        bool delivered = objective.TryDeliverOffering(item.ItemName);

        if (delivered)
        {
            item.gameObject.SetActive(false);
        }
    }

    public override void Interact()
    {
        if (objectivePanel == null)
            return;

        objectivePanel.SetActive(!objectivePanel.activeSelf);
    }
}