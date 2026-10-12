using UnityEngine;

public class Urn : PickupItem
{
    private static readonly Color FilledColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private PlayerDeath contained;
    private SpriteRenderer sprite;
    private Renderer rend;

    public bool IsFilled => contained != null;
    public PlayerDeath Contained => contained;

    public bool IsHeld
    {
        get
        {
            Transform parent = transform.parent;
            return parent != null && parent.GetComponentInParent<PlayerInventory>() != null;
        }
    }

    private void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        rend = GetComponentInChildren<Renderer>();
    }

    public override string GetPrompt()
    {
        if (IsFilled)
            return "Ambil gentong (abu " + contained.name + ")";

        return "Ambil gentong kosong";
    }

    public void Fill(PlayerDeath owner)
    {
        contained = owner;

        if (sprite != null)
            sprite.color = FilledColor;
        else if (rend != null)
            rend.material.color = FilledColor;

        DeadMarker.Create(transform, "Abu " + owner.name);
    }

    public void Consume()
    {
        Destroy(gameObject);
    }
}