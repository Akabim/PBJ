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

        if (item == null || item is DeadBody || objective == null)
            return;

        if (objective.IsComplete)
        {
            GameMessage.Show("Semua offering sudah terkumpul.");
            PushOut(item);
            return;
        }

        if (!objective.RequiredOfferings.Contains(item.ItemName))
        {
            GameMessage.Show("Shaman tidak meminta " + item.ItemName + ".");
            PushOut(item);
            return;
        }

        if (objective.DeliveredOfferings.Contains(item.ItemName))
        {
            GameMessage.Show(item.ItemName + " sudah diserahkan.");
            PushOut(item);
            return;
        }

        if (objective.TryDeliverOffering(item.ItemName))
        {
            item.gameObject.SetActive(false);

            if (objective.IsComplete)
                GameMessage.Show("Objective selesai!", 3f);
            else
                GameMessage.Show(
                    "Shaman menerima " + item.ItemName +
                    " (" + objective.Progress + "/" + objective.Total + ")"
                );
        }
    }

    public override void Interact()
    {
        if (objectivePanel == null)
            return;

        objectivePanel.SetActive(!objectivePanel.activeSelf);
    }

    public override string GetPrompt()
    {
        return "Lihat objective. Drop offering di sini (Q)";
    }

    private void PushOut(PickupItem item)
    {
        Collider area = GetComponent<Collider>();

        Vector3 center = area != null ? area.bounds.center : transform.position;
        float radius = area != null
            ? Mathf.Max(area.bounds.extents.x, area.bounds.extents.z)
            : 1f;

        Vector3 direction = item.transform.position - center;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = -transform.forward;

        Vector3 target = center + direction.normalized * (radius + 1f);
        target.y = item.transform.position.y;

        item.transform.position = target;
    }
}