using UnityEngine;

public class AltarRevive : MonoBehaviour
{
    [SerializeField] private Transform revivePoint;
    [SerializeField] private float radius = 3f;
    [SerializeField] private Color ringColor = new Color(0.3f, 0.9f, 1f, 1f);
    [SerializeField] private Color flashColor = Color.white;

    private GameObject ring;
    private Renderer ringRenderer;
    private Collider ownCollider;
    private float flashUntil;

    private void Start()
    {
        ownCollider = GetComponent<Collider>();
        CreateRing();
    }

    private void OnDestroy()
    {
        if (ring != null)
            Destroy(ring);
    }

    private void Update()
    {
        UpdateRing();

        if (PlayerDeath.DeadPlayers.Count == 0)
            return;

        Urn urn = FindFilledUrnInRange();
        if (urn == null)
            return;

        PlayerDeath owner = urn.Contained;
        if (owner == null || !owner.IsDead)
            return;

        Vector3 target = revivePoint != null
            ? revivePoint.position
            : transform.position + Vector3.up;

        urn.Consume();
        owner.Revive(target);

        flashUntil = Time.time + 0.6f;
    }

    private Urn FindFilledUrnInRange()
    {
        foreach (Urn urn in FindObjectsByType<Urn>(FindObjectsSortMode.None))
        {
            if (!urn.IsFilled || urn.IsHeld)
                continue;

            Vector3 a = transform.position;
            Vector3 b = urn.transform.position;
            a.y = 0f;
            b.y = 0f;

            if (Vector3.Distance(a, b) <= radius)
                return urn;
        }

        return null;
    }

    private void CreateRing()
    {
        ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "AltarRing";

        Collider ringCollider = ring.GetComponent<Collider>();
        if (ringCollider != null)
            Destroy(ringCollider);

        ringRenderer = ring.GetComponent<Renderer>();
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringRenderer.receiveShadows = false;
        ringRenderer.material.color = ringColor;
    }

    private void UpdateRing()
    {
        if (ring == null)
            return;

        float groundY = ownCollider != null
            ? ownCollider.bounds.min.y
            : transform.position.y - 0.5f;

        ring.transform.position = new Vector3(
            transform.position.x,
            groundY + 0.02f,
            transform.position.z
        );

        ring.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

        Color color;

        if (Time.time < flashUntil)
        {
            color = Color.Lerp(ringColor, flashColor, (flashUntil - Time.time) / 0.6f);
        }
        else
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 2f);
            color = ringColor * pulse;
            color.a = 1f;
        }

        ringRenderer.material.color = color;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}