using System.Collections;
using UnityEngine;
using CallKitty.Gameplay;

public class mainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject gamePanel;

    [Header("Play Delay")]
    [SerializeField] private float dealDelaySeconds = 2f;

    public void OnPlayButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartCoroutine(OpenGameAfterDelay());
        }
        else
        {
            StartCoroutine(OpenGameAfterDelay());
        }

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        if (gamePanel != null)
        {
            gamePanel.SetActive(true);
        }
    }

    private IEnumerator OpenGameAfterDelay()
    {
        yield return new WaitForSeconds(dealDelaySeconds);

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[mainMenu] GameManager instance not found.");
            yield break;
        }

        GameManager.Instance.StartGame();
    }

    public void QuitButtonClicked()
    {
        Application.Quit();
    }
}
