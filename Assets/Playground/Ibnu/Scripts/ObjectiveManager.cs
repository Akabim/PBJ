using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    [SerializeField]
    private List<string> offeringPool = new List<string>
    {
        "Batu", "ndog", "jagung", "mie", "geprek", "nasgor"

    };

    [SerializeField] private int offeringCount = 6;

    private List<string> requiredOfferings = new List<string>();
    private HashSet<string> deliveredOfferings = new HashSet<string>();

    public List<string> RequiredOfferings => requiredOfferings;
    public HashSet<string> DeliveredOfferings => deliveredOfferings;

    public int Progress => deliveredOfferings.Count;
    public int Total => requiredOfferings.Count;
    public bool IsComplete => Progress >= Total;

    private void Awake()
    {
        GenerateObjective();
    }

    public void GenerateObjective()
    {
        requiredOfferings.Clear();
        deliveredOfferings.Clear();

        List<string> shuffled = offeringPool.OrderBy(x => Random.value).ToList();
        int count = Mathf.Min(offeringCount, shuffled.Count);

        for (int i = 0; i < count; i++)
        {
            requiredOfferings.Add(shuffled[i]);
        }

        Debug.Log("Objective baru: " + string.Join(", ", requiredOfferings));
    }

    public bool TryDeliverOffering(string offeringName)
    {
        if (!requiredOfferings.Contains(offeringName))
            return false;

        if (deliveredOfferings.Contains(offeringName))
            return false;

        deliveredOfferings.Add(offeringName);

        Debug.Log($"Offering diterima: {offeringName} ({Progress}/{Total})");

        if (IsComplete)
        {
            Debug.Log("Semua offering terkumpul! Objective selesai.");
        }

        return true;
    }
}