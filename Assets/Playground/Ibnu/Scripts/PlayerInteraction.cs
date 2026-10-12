using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRange = 2f;

    private PlayerInventory inventory;
    private InteractableObject currentTarget;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    private void OnDisable()
    {
        currentTarget = null;
        GameMessage.SetPrompt("");
    }

    private void Update()
    {
        FindTarget();

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame && currentTarget != null)
        {
            currentTarget.Interact(gameObject);
        }
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactionRange
        );

        InteractableObject best = null;
        float bestDistance = float.MaxValue;
        bool blockedByFullHands = false;

        foreach (Collider hit in hits)
        {
            InteractableObject candidate =
                hit.GetComponentInParent<InteractableObject>();

            if (candidate == null || !candidate.isActiveAndEnabled)
                continue;

            if (candidate.gameObject == gameObject)
                continue;

            if (candidate.transform.IsChildOf(transform))
                continue;

            if (!candidate.CanInteract(gameObject))
                continue;

            if (inventory != null && inventory.IsHolding && candidate is PickupItem)
            {
                blockedByFullHands = true;
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                candidate.transform.position
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        currentTarget = best;

        if (best != null)
            GameMessage.SetPrompt("E: " + best.GetPrompt());
        else if (blockedByFullHands)
            GameMessage.SetPrompt("Tangan penuh. Q untuk drop");
        else
            GameMessage.SetPrompt("");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            interactionRange
        );
    }
}