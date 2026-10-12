using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 40f;

    private Rigidbody rb;
    private Vector3 inputDirection;

    public Vector3 InputDirection => inputDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnDisable()
    {
        inputDirection = Vector3.zero;
    }

    private void Update()
    {
        inputDirection = ReadInput();
    }

    private void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);

        Vector3 target = inputDirection * moveSpeed;
        float rate = inputDirection.sqrMagnitude > 0.01f ? acceleration : deceleration;

        horizontal = Vector3.MoveTowards(
            horizontal,
            target,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
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