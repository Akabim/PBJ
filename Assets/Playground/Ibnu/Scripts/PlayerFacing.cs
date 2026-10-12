using UnityEngine;

public class PlayerFacing : MonoBehaviour
{
    [SerializeField] private float turnSpeedDegrees = 720f;

    private Rigidbody rb;
    private PlayerMovement movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
    }

    private void FixedUpdate()
    {
        if (rb == null || movement == null)
            return;

        Vector3 direction = movement.InputDirection;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);

        rb.MoveRotation(
            Quaternion.RotateTowards(
                rb.rotation,
                target,
                turnSpeedDegrees * Time.fixedDeltaTime
            )
        );
    }
}