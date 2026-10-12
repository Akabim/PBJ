using TMPro;
using UnityEngine;

public class DeadMarker : MonoBehaviour
{
    private const float FontSize = 6f;
    private const float Height = 2.2f;

    private Transform target;
    private TextMeshPro label;

    public static DeadMarker Create(Transform follow, string text)
    {
        GameObject go = new GameObject("Marker_" + follow.name);
        DeadMarker marker = go.AddComponent<DeadMarker>();

        marker.target = follow;
        marker.label = go.AddComponent<TextMeshPro>();
        marker.label.text = text;
        marker.label.alignment = TextAlignmentOptions.Center;
        marker.label.fontSize = FontSize;
        marker.label.fontStyle = FontStyles.Bold;
        marker.label.color = new Color(1f, 0.85f, 0.2f, 1f);
        marker.label.sortingOrder = 100;
        marker.label.rectTransform.sizeDelta = new Vector2(14f, 2f);

        return marker;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        float bob = Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.position = target.position + Vector3.up * (Height + bob);

        Camera cam = Camera.main;

        if (cam != null)
            transform.rotation = cam.transform.rotation;
    }
}