using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(NavMeshAgent))]
public class HunterChase : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float patrolSpeed = 1.2f;
    [SerializeField] private float repathInterval = 0.2f;

    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float loseRange = 18f;
    [SerializeField] private float patrolRadius = 10f;

    [FormerlySerializedAs("killDistance")]
    [SerializeField] private float attackDistance = 1.2f;
    [SerializeField] private float windupSeconds = 0.6f;
    [SerializeField] private float dodgeLeeway = 0.6f;
    [SerializeField] private float hitPauseSeconds = 0.8f;
    [SerializeField] private Color windupColor = new Color(1f, 0.25f, 0.25f, 1f);

    private enum State { Patrol, Chase, Windup, Recover }

    private State state = State.Patrol;
    private NavMeshAgent agent;
    private SpriteRenderer sprite;
    private Color originalColor = Color.white;
    private Vector3 originalScale;
    private PlayerDeath target;
    private float nextRepath;
    private float stateUntil;
    private float patrolWaitUntil;
    private bool warned;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 0f;
        agent.updateRotation = false;

        sprite = GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            originalColor = sprite.color;

        originalScale = transform.localScale;
    }

    private void OnDisable()
    {
        ClearVisual();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh)
        {
            if (!warned)
            {
                Debug.LogWarning("Hunter tidak berada di NavMesh. Bake NavMesh dulu.");
                warned = true;
            }
            return;
        }

        switch (state)
        {
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.Windup:
                UpdateWindup();
                break;
            case State.Recover:
                UpdateRecover();
                break;
        }
    }

    private void UpdatePatrol()
    {
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        PlayerDeath seen = FindNearestPlayer(detectionRange);

        if (seen != null)
        {
            target = seen;
            state = State.Chase;
            return;
        }

        bool arrived = !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= 0.5f);

        if (!arrived)
            return;

        if (patrolWaitUntil <= 0f)
            patrolWaitUntil = Time.time + 1f;

        if (Time.time < patrolWaitUntil)
            return;

        patrolWaitUntil = 0f;
        PickPatrolPoint();
    }

    private void PickPatrolPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    private void UpdateChase()
    {
        agent.isStopped = false;
        agent.speed = speed;

        if (target == null || target.IsDead || target.IsProtected)
        {
            target = FindNearestPlayer(loseRange);

            if (target == null)
            {
                EnterPatrol();
                return;
            }
        }
        else if (FlatDistance(transform.position, target.transform.position) > loseRange)
        {
            EnterPatrol();
            return;
        }

        if (Time.time >= nextRepath)
        {
            agent.SetDestination(target.transform.position);
            nextRepath = Time.time + repathInterval;
        }

        if (target.CanBeHit &&
            FlatDistance(transform.position, target.transform.position) <= attackDistance)
        {
            StartWindup();
        }
    }

    private void StartWindup()
    {
        state = State.Windup;
        stateUntil = Time.time + windupSeconds;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private void UpdateWindup()
    {
        if (target == null || target.IsDead)
        {
            ClearVisual();
            state = State.Chase;
            return;
        }

        float progress = Mathf.Clamp01(1f - (stateUntil - Time.time) / windupSeconds);
        ApplyWindupVisual(progress);

        if (Time.time < stateUntil)
            return;

        ClearVisual();

        bool stillClose =
            FlatDistance(transform.position, target.transform.position) <= attackDistance + dodgeLeeway;

        if (stillClose && target.CanBeHit)
            target.TakeHit();

        state = State.Recover;
        stateUntil = Time.time + hitPauseSeconds;
    }

    private void UpdateRecover()
    {
        agent.isStopped = true;

        if (Time.time >= stateUntil)
            state = State.Chase;
    }

    private void EnterPatrol()
    {
        state = State.Patrol;
        target = null;
        patrolWaitUntil = 0f;
        agent.ResetPath();
    }

    private void ApplyWindupVisual(float progress)
    {
        if (sprite != null)
        {
            sprite.color = Color.Lerp(
                originalColor,
                windupColor,
                Mathf.PingPong(Time.time * 8f, 1f)
            );
        }

        transform.localScale = originalScale * (1f + 0.25f * progress);
    }

    private void ClearVisual()
    {
        if (sprite != null)
            sprite.color = originalColor;

        transform.localScale = originalScale;
    }

    private PlayerDeath FindNearestPlayer(float range)
    {
        PlayerDeath nearest = null;
        float bestDistance = range;

        foreach (PlayerDeath player in FindObjectsByType<PlayerDeath>(FindObjectsSortMode.None))
        {
            if (player.IsDead || player.IsProtected)
                continue;

            float distance = FlatDistance(transform.position, player.transform.position);

            if (distance <= bestDistance)
            {
                bestDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}