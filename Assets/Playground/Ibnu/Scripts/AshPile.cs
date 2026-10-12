using UnityEngine;

public class AshPile : InteractableObject
{
    private PlayerDeath owner;

    public PlayerDeath Owner => owner;

    public static AshPile Create(PlayerDeath owner, Vector3 groundPoint)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Abu_" + owner.name;

        go.transform.position = groundPoint + Vector3.up * 0.12f;
        go.transform.localScale = new Vector3(1.2f, 0.25f, 1.2f);

        Renderer rend = go.GetComponent<Renderer>();
        rend.material.color = new Color(0.45f, 0.45f, 0.45f, 1f);

        Collider col = go.GetComponent<Collider>();
        col.isTrigger = true;

        AshPile pile = go.AddComponent<AshPile>();
        pile.owner = owner;

        DeadMarker.Create(go.transform, "Butuh gentong");

        return pile;
    }

    public override bool CanInteract(GameObject interactor)
    {
        return GetEmptyUrn(interactor) != null;
    }

    public override string GetPrompt()
    {
        return "Masukkan abu " + owner.name + " ke gentong";
    }

    public override void Interact(GameObject interactor)
    {
        Urn urn = GetEmptyUrn(interactor);

        if (urn == null)
            return;

        urn.Fill(owner);

        GameMessage.Show("Abu " + owner.name + " masuk ke gentong. Bawa ke Altar.", 3f);

        Destroy(gameObject);
    }

    private Urn GetEmptyUrn(GameObject interactor)
    {
        if (interactor == null || owner == null)
            return null;

        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();

        if (inventory == null)
            return null;

        Urn urn = inventory.HeldItem as Urn;

        if (urn == null || urn.IsFilled)
            return null;

        return urn;
    }
}