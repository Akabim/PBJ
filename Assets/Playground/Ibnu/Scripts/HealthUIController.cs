
using UnityEngine;
using UnityEngine.UI;

public class HealthUIController : MonoBehaviour
{
    [Header("Health UI Image")]
    [SerializeField] private Image healthImage;

    [Header("Health Sprites")]
    [SerializeField] private Sprite health0;
    [SerializeField] private Sprite health1;
    [SerializeField] private Sprite health2;
    [SerializeField] private Sprite health3;

    private PlayerDeath playerDeath;

    private void OnEnable()
    {
        FindPlayerHealth();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayer();
    }

    private void FindPlayerHealth()
    {
        PlayerDeath[] players = FindObjectsByType<PlayerDeath>(
            FindObjectsSortMode.None
        );

        foreach (PlayerDeath player in players)
        {
            if (player.CompareTag("Player"))
            {
                playerDeath = player;
                break;
            }
        }

        if (playerDeath == null && players.Length > 0)
            playerDeath = players[0];

        if (playerDeath == null)
        {
            Debug.LogWarning("HealthUI: PlayerDeath belum ditemukan.");
            return;
        }

        playerDeath.Hit += UpdateHealth;
        playerDeath.Died += UpdateHealth;
        playerDeath.Revived += UpdateHealth;

        UpdateHealth(playerDeath);
    }

    private void UnsubscribeFromPlayer()
    {
        if (playerDeath == null)
            return;

        playerDeath.Hit -= UpdateHealth;
        playerDeath.Died -= UpdateHealth;
        playerDeath.Revived -= UpdateHealth;
        playerDeath = null;
    }

    private void UpdateHealth(PlayerDeath player)
    {
        int hp = player.HitsRemaining;

        if (hp >= 3)
            healthImage.sprite = health3;
        else if (hp == 2)
            healthImage.sprite = health2;
        else if (hp == 1)
            healthImage.sprite = health1;
        else
            healthImage.sprite = health0;

        healthImage.enabled = true;
    }
}
