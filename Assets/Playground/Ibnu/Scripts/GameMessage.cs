using TMPro;
using UnityEngine;

public class GameMessage : MonoBehaviour
{
    private static GameMessage instance;

    [SerializeField] private TMP_Text label;

    private string prompt = "";
    private string message = "";
    private float messageUntil;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (message != "" && Time.time >= messageUntil)
            message = "";

        if (label == null)
            return;

        string text = message != "" ? message : prompt;

        if (label.text != text)
            label.text = text;
    }

    public static void SetPrompt(string text)
    {
        if (instance != null)
            instance.prompt = text ?? "";
    }

    public static void Show(string text, float seconds = 2f)
    {
        if (instance == null)
        {
            Debug.Log(text);
            return;
        }

        instance.message = text;
        instance.messageUntil = Time.time + seconds;
    }
}