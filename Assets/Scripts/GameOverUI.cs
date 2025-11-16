using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    public GameObject gameOverOverlay;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI survivedText;
    public TextMeshProUGUI gameOverScoreText;

    public Button restartButton;
    public Button mainMenuButton; // optional

    private bool isGameOver = false;
    private float survivedTime = 0f;

    [Header("UI Elements to Hide On Game Over")]
    public GameObject[] uiToHide;     // <— NEU für deine Boosts & Score UI

    void Start()
    {
        // Game Over UI verstecken
        gameOverOverlay.SetActive(false);
        gameOverText.gameObject.SetActive(false);
        survivedText.gameObject.SetActive(false);
        gameOverScoreText.gameObject.SetActive(false);

        restartButton.gameObject.SetActive(false);
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);

        restartButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(BackToMainMenu);

        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!isGameOver)
            survivedTime += Time.deltaTime;

        if (!isGameOver && playerHealth.currentHealth <= 0)
        {
            ShowGameOver();
        }
    }

    void ShowGameOver()
    {
        isGameOver = true;

        // 1. Alle HUD-Elemente ausblenden (Score, Boosts, Popups etc.)
        foreach (GameObject ui in uiToHide)
        {
            if (ui != null)
                ui.SetActive(false);
        }

        // 2. Game Over UI einschalten
        gameOverOverlay.SetActive(true);
        gameOverText.gameObject.SetActive(true);
        survivedText.gameObject.SetActive(true);
        gameOverScoreText.gameObject.SetActive(true);

        restartButton.gameObject.SetActive(true);
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(true);

        // 3. Überlebenszeit formatieren
        int minutes = Mathf.FloorToInt(survivedTime / 60f);
        int seconds = Mathf.FloorToInt(survivedTime % 60f);
        survivedText.text = $"You survived: {minutes:00}:{seconds:00}";

        // 4. Score anzeigen
        gameOverScoreText.text = $"Score: {ScoreManager.Instance.GetScore()}";

        // 5. Spiel einfrieren
        Time.timeScale = 0f;
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
