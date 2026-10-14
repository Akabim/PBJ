using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Gerak")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 40f;

    [Header("Sprint")]
    [SerializeField] private float sprintMultiplier = 1.6f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float drainPerSecond = 25f;
    [SerializeField] private float regenPerSecond = 15f;
    [SerializeField] private float regenDelay = 1f;
    [SerializeField] private float recoverThreshold = 20f;

    [Header("Gentong berisi abu")]
    [SerializeField] private float loadedMultiplier = 0.6f;

    private Rigidbody rb;
    private PlayerInventory inventory;
    private Vector3 inputDirection;

    private float stamina;
    private float regenTimer;
    private bool exhausted;
    private bool isSprinting;

    public Vector3 InputDirection => inputDirection;
    public float Stamina01 => stamina / maxStamina;
    public bool IsExhausted => exhausted;
    public bool IsSprinting => isSprinting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
            inventory = GetComponentInChildren<PlayerInventory>();
        if (inventory == null)
            inventory = GetComponentInParent<PlayerInventory>();

        stamina = maxStamina;
    }

    private void OnDisable()
    {
        inputDirection = Vector3.zero;
        isSprinting = false;
    }

    private void Update()
    {
        inputDirection = ReadInput();
        UpdateStamina();
    }

    private void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);

        Vector3 target = inputDirection * CurrentSpeed();
        float rate = inputDirection.sqrMagnitude > 0.01f ? acceleration : deceleration;

        horizontal = Vector3.MoveTowards(
            horizontal,
            target,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
    }

    private float CurrentSpeed()
    {
        if (IsCarryingAsh())
            return moveSpeed * loadedMultiplier;

        if (isSprinting)
            return moveSpeed * sprintMultiplier;

        return moveSpeed;
    }

    private bool IsCarryingAsh()
    {
        if (inventory == null || inventory.HeldItem == null)
            return false;

        Urn urn = inventory.HeldItem as Urn;
        return urn != null && urn.IsFilled;
    }

    private void UpdateStamina()
    {
        Keyboard keyboard = Keyboard.current;
        bool shiftHeld = keyboard != null && keyboard.leftShiftKey.isPressed;
        bool moving = inputDirection.sqrMagnitude > 0.01f;

        bool wantsSprint = shiftHeld && moving && !exhausted && !IsCarryingAsh();

        if (wantsSprint)
        {
            isSprinting = true;
            stamina -= drainPerSecond * Time.deltaTime;
            regenTimer = regenDelay;

            if (stamina <= 0f)
            {
                stamina = 0f;
                exhausted = true;
                isSprinting = false;
            }
            return;
        }

        isSprinting = false;

        if (regenTimer > 0f)
        {
            regenTimer -= Time.deltaTime;
        }
        else
        {
            stamina = Mathf.Min(maxStamina, stamina + regenPerSecond * Time.deltaTime);
        }

        if (exhausted && stamina >= recoverThreshold)
            exhausted = false;
    }

    private Vector3 ReadInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return Vector3.zero;

        Vector3 input = Vector3.zero;

        if (keyboard.wKey.isPressed)
            input.z += 1f;

        if (keyboard.sKey.isPressed)
            input.z -= 1f;

        if (keyboard.aKey.isPressed)
            input.x -= 1f;

        if (keyboard.dKey.isPressed)
            input.x += 1f;

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        return input;
    }
}