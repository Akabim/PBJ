using UnityEngine;

public class HunterChase : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float killDistance = 1.2f;

    private void Update()
    {
        PlayerDeath target = FindNearestAlivePlayer();

        if (target == null)
            return;

        Vector3 targetPosition = target.transform.position;
        targetPosition.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) <= killDistance)
            target.Die();
    }

    private PlayerDeath FindNearestAlivePlayer()
    {
        PlayerDeath nearest = null;
        float bestDistance = float.MaxValue;

        foreach (PlayerDeath player in FindObjectsByType<PlayerDeath>(FindObjectsSortMode.None))
        {
            if (player.IsDead)
                continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }
}