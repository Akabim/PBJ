using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDeath : MonoBehaviour
{
    public static readonly List<PlayerDeath> DeadPlayers = new List<PlayerDeath>();

    [SerializeField] private GameObject deathMessage;
    [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float hitCooldownSeconds = 1.5f;
    [SerializeField] private float reviveProtectionSeconds = 2f;
    [SerializeField] private bool enableTestKey = true;

    public bool IsDead { get; private set; }
    public int HitsTaken { get; private set; }
    public int MaxHits => maxHits;
    public int HitsRemaining => Mathf.Max(0, maxHits - HitsTaken);

    public bool IsProtected => Time.time < protectedUntil;
    public bool CanBeHit => !IsDead && !IsProtected && Time.time >= hitCooldownUntil;

    public event Action<PlayerDeath> Hit;
    public event Action<PlayerDeath> Died;
    public event Action<PlayerDeath> Revived;

    private readonly List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private readonly List<Collider> disabledColliders = new List<Collider>();

    private AshPile ash;
    private SpriteRenderer sprite;
    private Renderer rend;
    private Color originalColor;
    private float protectedUntil;
    private float hitCooldownUntil;
    private bool wasKinematic;
    private Coroutine flashRoutine;

    private void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        rend = GetComponentInChildren<Renderer>();

        if (sprite != null)
            originalColor = sprite.color;
        else if (rend != null)
            originalColor = rend.material.color;
    }

    private void Update()
    {
        if (!enableTestKey || Keyboard.current == null)
            return;

        if (Keyboard.current.kKey.wasPressedThisFrame)
            Die(true);

        if (Keyboard.current.hKey.wasPressedThisFrame)
            TakeHit();
    }

    public bool TakeHit()
    {
        if (!CanBeHit)
            return false;

        HitsTaken++;
        hitCooldownUntil = Time.time + hitCooldownSeconds;
        Hit?.Invoke(this);

        if (HitsTaken >= maxHits)
        {
            Die(true);
            return true;
        }

        GameMessage.Show(name + " kena hit (" + HitsTaken + "/" + maxHits + ")");

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HitFlash());

        return true;
    }

    private IEnumerator HitFlash()
    {
        SetTint(hitColor);
        yield return new WaitForSeconds(0.15f);

        SetTint(originalColor);
        flashRoutine = null;
    }

    public void Die(bool force = false)
    {
        if (IsDead)
            return;

        if (IsProtected && !force)
            return;

        IsDead = true;
        HitsTaken = maxHits;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        SetTint(originalColor);

        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory != null)
            inventory.Drop();

        Vector3 groundPoint = GetGroundPoint();

        disabledScripts.Clear();
        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
        {
            if (script == this || !script.enabled)
                continue;

            script.enabled = false;
            disabledScripts.Add(script);
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;

            if (!rb.isKinematic)
                rb.linearVelocity = Vector3.zero;

            rb.isKinematic = true;
        }

        hiddenRenderers.Clear();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled)
                continue;

            r.enabled = false;
            hiddenRenderers.Add(r);
        }

        disabledColliders.Clear();
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (!c.enabled)
                continue;

            c.enabled = false;
            disabledColliders.Add(c);
        }

        ash = AshPile.Create(this, groundPoint);

        if (deathMessage != null)
            deathMessage.SetActive(true);

        DeadPlayers.Add(this);
        Died?.Invoke(this);

        GameMessage.Show(name + " jadi abu. Cari gentong untuk membawanya ke Altar.", 3f);
    }

    public void Revive(Vector3 position)
    {
        if (!IsDead)
            return;

        if (ash != null)
        {
            Destroy(ash.gameObject);
            ash = null;
        }

        transform.position = position;

        foreach (Collider c in disabledColliders)
        {
            if (c != null)
                c.enabled = true;
        }
        disabledColliders.Clear();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = wasKinematic;

        foreach (Renderer r in hiddenRenderers)
        {
            if (r != null)
                r.enabled = true;
        }
        hiddenRenderers.Clear();

        foreach (MonoBehaviour script in disabledScripts)
        {
            if (script != null)
                script.enabled = true;
        }
        disabledScripts.Clear();

        SetTint(originalColor);

        if (deathMessage != null)
            deathMessage.SetActive(false);

        IsDead = false;
        HitsTaken = 0;
        protectedUntil = Time.time + reviveProtectionSeconds;

        DeadPlayers.Remove(this);
        Revived?.Invoke(this);

        GameMessage.Show(name + " hidup lagi.");
    }

    private Vector3 GetGroundPoint()
    {
        Collider col = GetComponent<Collider>();

        float y = col != null
            ? col.bounds.min.y
            : transform.position.y - 0.5f;

        return new Vector3(transform.position.x, y, transform.position.z);
    }

    private void SetTint(Color color)
    {
        if (sprite != null)
            sprite.color = color;
        else if (rend != null)
            rend.material.color = color;
    }

    private void OnDestroy()
    {
        DeadPlayers.Remove(this);
    }
}