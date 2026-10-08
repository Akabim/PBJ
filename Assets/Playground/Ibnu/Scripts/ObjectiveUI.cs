using UnityEngine;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private TMP_Text objectiveText;

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (objectiveManager == null || objectiveText == null)
            return;

        if (objectiveManager.IsComplete)
        {
            objectiveText.text = "Objective selesai";
            return;
        }

        string result = $"Offering ({objectiveManager.Progress}/{objectiveManager.Total})\n";

        foreach (string offering in objectiveManager.RequiredOfferings)
        {
            bool done = objectiveManager.DeliveredOfferings.Contains(offering);
            result += (done ? "[v] " : "[ ] ") + offering + "\n";
        }

        objectiveText.text = result;
    }

    public void TogglePanel()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}