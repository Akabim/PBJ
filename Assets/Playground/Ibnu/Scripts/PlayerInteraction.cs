using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRange = 2f;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Collider[] objects = Physics.OverlapSphere(
            transform.position,
            interactionRange
        );

        foreach (Collider obj in objects)
        {
            InteractableObject interactable =
                obj.GetComponent<InteractableObject>();

            if (interactable != null)
            {
                interactable.Interact(this);
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            interactionRange
        );
    }
}