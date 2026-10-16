
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;

    public void OpenHostPanel()
    {
        mainMenuPanel.SetActive(false);
        hostPanel.SetActive(true);
        joinPanel.SetActive(false);
    }

    public void OpenJoinPanel()
    {
        mainMenuPanel.SetActive(false);
        hostPanel.SetActive(false);
        joinPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        hostPanel.SetActive(false);
        joinPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Exit Game dipanggil.");
    }
}
