using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public virtual void Interact()
    {
        Debug.Log("Object berhasil di-interact!");
    }

    public virtual void Interact(GameObject interactor)
    {
        Interact();
    }

    public virtual string GetPrompt()
    {
        return "Interaksi";
    }

    public virtual bool CanInteract(GameObject interactor)
    {
        return true;
    }
}