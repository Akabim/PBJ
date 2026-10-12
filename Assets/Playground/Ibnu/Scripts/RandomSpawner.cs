using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RandomSpawner : MonoBehaviour
{
    [SerializeField] private Collider spawnArea;
    [SerializeField] private float edgeMargin = 4f;
    [SerializeField] private Transform[] fixedPoints;
    [SerializeField] private Transform altar;
    [SerializeField] private HunterChase hunter;
    [SerializeField] private bool randomizeItems = true;
    [SerializeField] private float altarMinDistance = 15f;
    [SerializeField] private float itemMinDistance = 6f;
    [SerializeField] private float hunterMinDistance = 20f;
    [SerializeField] private float spacing = 4f;

    private readonly List<Vector3> fixedPositions = new List<Vector3>();
    private readonly List<Vector3> used = new List<Vector3>();

    private void Start()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("Spawn Area belum diisi di RandomSpawner.");
            return;
        }

        foreach (Transform point in fixedPoints)
        {
            if (point == null)
                continue;

            fixedPositions.Add(point.position);
            used.Add(point.position);
        }

        PlaceAltar();
        PlaceItems();
        PlaceHunter();
    }

    private void PlaceAltar()
    {
        if (altar == null)
            return;

        if (!TryFindPoint(altarMinDistance, out Vector3 point))
            return;

        Vector3 position = altar.position;
        position.x = point.x;
        position.z = point.z;
        altar.position = position;

        used.Add(point);
    }

    private void PlaceItems()
    {
        if (!randomizeItems)
            return;

        foreach (PickupItem item in FindObjectsByType<PickupItem>(FindObjectsSortMode.None))
        {
            if (item is DeadBody)
                continue;

            if (!TryFindPoint(itemMinDistance, out Vector3 point))
                continue;

            Vector3 position = item.transform.position;
            position.x = point.x;
            position.z = point.z;
            item.transform.position = position;

            used.Add(point);
        }
    }

    private void PlaceHunter()
    {
        if (hunter == null)
            return;

        if (!TryFindPoint(hunterMinDistance, out Vector3 point))
            return;

        NavMeshAgent agent = hunter.GetComponent<NavMeshAgent>();

        if (agent != null)
            agent.enabled = false;

        hunter.transform.position = point;

        if (agent != null)
            agent.enabled = true;
    }

    private bool TryFindPoint(float minFromFixed, out Vector3 result)
    {
        Bounds bounds = spawnArea.bounds;

        Vector3 best = Vector3.zero;
        float bestScore = -1f;

        for (int i = 0; i < 60; i++)
        {
            Vector3 random = new Vector3(
                Random.Range(bounds.min.x + edgeMargin, bounds.max.x - edgeMargin),
                bounds.max.y,
                Random.Range(bounds.min.z + edgeMargin, bounds.max.z - edgeMargin)
            );

            if (!NavMesh.SamplePosition(random, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                continue;

            Vector3 point = hit.position;

            if (NearestDistance(point, used) < spacing)
                continue;

            float nearestFixed = NearestDistance(point, fixedPositions);

            if (nearestFixed >= minFromFixed)
            {
                result = point;
                return true;
            }

            if (nearestFixed > bestScore)
            {
                bestScore = nearestFixed;
                best = point;
            }
        }

        if (bestScore >= 0f)
        {
            result = best;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private float NearestDistance(Vector3 point, List<Vector3> others)
    {
        float best = float.MaxValue;

        foreach (Vector3 other in others)
        {
            Vector3 a = point;
            Vector3 b = other;
            a.y = 0f;
            b.y = 0f;

            best = Mathf.Min(best, Vector3.Distance(a, b));
        }

        return best;
    }
}