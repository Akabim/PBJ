using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDeath : MonoBehaviour
{
    public static readonly List<PlayerDeath> DeadPlayers = new List<PlayerDeath>();

    [SerializeField] private GameObject deathMessage;
    [SerializeField] private Color deathColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private bool enableTestKey = true;

    public bool IsDead { get; private set; }
    public bool IsCarried => transform.parent != null;

    public event Action<PlayerDeath> Died;
    public event Action<PlayerDeath> Revived;

    private readonly List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
    private DeadBody body;
    private SpriteRenderer sprite;
    private Renderer rend;
    private Color originalColor;

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
            Die();
    }

    public void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory != null)
            inventory.Drop();

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
            rb.linearVelocity = Vector3.zero;

        body = gameObject.AddComponent<DeadBody>();

        SetTint(deathColor);

        if (deathMessage != null)
            deathMessage.SetActive(true);

        DeadPlayers.Add(this);
        Died?.Invoke(this);

        Debug.Log("Player mati.");
    }

    public void Revive(Vector3 position)
    {
        if (!IsDead)
            return;

        if (body != null)
            Destroy(body);

        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        transform.position = position;

        if (controller != null)
            controller.enabled = true;

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
        DeadPlayers.Remove(this);
        Revived?.Invoke(this);

        Debug.Log("Player hidup kembali.");
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