
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Image fill;

    [SerializeField] private Color normalColor =
        new Color(0.95f, 0.8f, 0.2f, 1f);

    [SerializeField] private Color exhaustedColor =
        new Color(0.85f, 0.2f, 0.2f, 1f);

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        if (player == null || fill == null)
            return;

        float stamina = Mathf.Clamp01(player.Stamina01);

        RectTransform rt = fill.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(stamina, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fill.color = player.IsExhausted
            ? exhaustedColor
            : normalColor;
    }
}
