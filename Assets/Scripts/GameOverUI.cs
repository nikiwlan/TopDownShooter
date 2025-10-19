using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public TextMeshProUGUI gameOverText;
    public Button restartButton;
 

    void Start()
    {
        // Text und Button am Anfang verstecken
        gameOverText.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);

        // Button-Funktion verbinden
        restartButton.onClick.AddListener(RestartGame);
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            gameOverText.gameObject.SetActive(true);
            restartButton.gameObject.SetActive(true);
        }
    }

    void RestartGame()
    {
        // Aktuelle Szene neu laden
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
