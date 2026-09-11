using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private float fallThresholdY = -10f;
    [SerializeField] private Vector3 respawnPosition = new Vector3(0f, 1f, 0f);

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (transform.position.y < fallThresholdY)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        transform.position = respawnPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }
}