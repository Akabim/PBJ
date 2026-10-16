
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Button")]
    [SerializeField] private float liftAmount = 10f;
    [SerializeField] private float animationSpeed = 12f;

    [Header("Hand for This Button")]
    [SerializeField] private RectTransform hand;
    [SerializeField] private float handLiftAmount = 12f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector2 handOriginalPosition;
    private Coroutine moveRoutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;

        if (hand != null)
            handOriginalPosition = hand.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateTo(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(false);
    }

    private void AnimateTo(bool hovering)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        Vector2 buttonTarget = hovering
            ? originalPosition + Vector2.up * liftAmount
            : originalPosition;

        Vector2 handTarget = hovering
            ? handOriginalPosition + Vector2.up * handLiftAmount
            : handOriginalPosition;

        moveRoutine = StartCoroutine(Move(buttonTarget, handTarget));
    }

    private IEnumerator Move(Vector2 buttonTarget, Vector2 handTarget)
    {
        while (Vector2.Distance(rectTransform.anchoredPosition,
                   buttonTarget) > 0.1f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                buttonTarget,
                Time.unscaledDeltaTime * animationSpeed);

            if (hand != null)
            {
                hand.anchoredPosition = Vector2.Lerp(
                    hand.anchoredPosition,
                    handTarget,
                    Time.unscaledDeltaTime * animationSpeed);
            }

            yield return null;
        }

        rectTransform.anchoredPosition = buttonTarget;

        if (hand != null)
            hand.anchoredPosition = handTarget;

        moveRoutine = null;
    }
}
