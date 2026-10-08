using UnityEngine;

public class AltarRevive : MonoBehaviour
{
    [SerializeField] private Transform revivePoint;
    [SerializeField] private float radius = 3f;
    [SerializeField] private bool requireMaterial = false;
    [SerializeField] private string reviveMaterialName = "ReviveMaterial";

    private void Update()
    {
        if (PlayerDeath.DeadPlayers.Count == 0)
            return;

        PlayerDeath deadPlayer = FindDeadPlayerInRange();
        if (deadPlayer == null)
            return;

        if (requireMaterial)
        {
            PickupItem material = FindMaterialInRange();
            if (material == null)
                return;

            material.gameObject.SetActive(false);
        }

        Vector3 target = revivePoint != null
            ? revivePoint.position
            : transform.position + Vector3.up;

        deadPlayer.Revive(target);

        Debug.Log("Altar merevive player.");
    }

    private PlayerDeath FindDeadPlayerInRange()
    {
        foreach (PlayerDeath dead in PlayerDeath.DeadPlayers)
        {
            if (dead.IsCarried)
                continue;

            if (Vector3.Distance(transform.position, dead.transform.position) <= radius)
                return dead;
        }

        return null;
    }

    private PickupItem FindMaterialInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in hits)
        {
            PickupItem item = hit.GetComponent<PickupItem>();

            if (item == null || item is DeadBody)
                continue;

            if (!item.gameObject.activeInHierarchy)
                continue;

            if (item.ItemName == reviveMaterialName)
                return item;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}