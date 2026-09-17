using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public virtual void Interact()
    {
        Debug.Log("Object berhasil di-interact!");
    }

    // Overload multiplayer: player pemicu interaksi diteruskan agar item bisa
    // masuk ke inventory milik player itu (bukan inventory pertama di scene).
    public virtual void Interact(PlayerInteraction player)
    {
        Interact();
    }
}