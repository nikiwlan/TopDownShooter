using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // WICHTIG für Coroutines

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public GameStateManager gameStateManager;

    public GameObject gameOverOverlay;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI survivedText;
    public TextMeshProUGUI gameOverScoreText;

    public Button restartButton;
    public Button mainMenuButton;

    private bool isGameOver = false;
    private float survivedTime = 0f;

    [Header("UI Elements to Hide On Game Over")]
    public GameObject[] uiToHide;

    void Start()
    {
        if (gameStateManager == null) gameStateManager = GameStateManager.Instance;

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
        {
            survivedTime += Time.deltaTime;
        }

        // ÄNDERUNG: Statt direkt ShowGameOver() zu rufen, starten wir die Verzögerung
        if (!isGameOver && playerHealth != null && playerHealth.currentHealth <= 0)
        {
            StartCoroutine(DelayedGameOver());
        }
    }

    // --- NEU: Die Verzögerung für Sound & Animation ---
    IEnumerator DelayedGameOver()
    {
        isGameOver = true; // Setzt Flag sofort, damit Update nicht nochmal feuert

        // Warte 2 Sekunden (passend zur Länge deiner Todes-Animation/Sounds)
        // Während dieser Zeit läuft das Spiel weiter (Animation ist sichtbar)
        yield return new WaitForSeconds(2.0f);

        // Erst JETZT das Menü anzeigen und Zeit stoppen
        ShowGameOver();
    }

    void ShowGameOver()
    {
        // isGameOver = true; // Das haben wir oben schon gesetzt, schadet aber nicht.

        // --- ZENTRALE LOGIK AUFRUFEN ---
        // Spiel stopp, Maus an, KEIN PauseMenu mehr erlaubt (false)
        if (gameStateManager != null)
            gameStateManager.SetGameState(true, false);

        // --- UI SPEZIFISCHES ZEUG ---

        // HUD verstecken
        foreach (GameObject ui in uiToHide)
        {
            if (ui != null)
                ui.SetActive(false);
        }

        // Game Over UI anzeigen
        gameOverOverlay.SetActive(true);
        gameOverText.gameObject.SetActive(true);
        survivedText.gameObject.SetActive(true);
        gameOverScoreText.gameObject.SetActive(true);

        restartButton.gameObject.SetActive(true);
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(true);

        // Zeit & Score anzeigen
        int minutes = Mathf.FloorToInt(survivedTime / 60f);
        int seconds = Mathf.FloorToInt(survivedTime % 60f);
        survivedText.text = $"You survived: {minutes:00}:{seconds:00}";

        int finalScore = 0;
        if (ScoreManager.Instance != null)
            finalScore = ScoreManager.Instance.GetScore();

        gameOverScoreText.text = $"Score: {finalScore}";

        // Highscore speichern
        // HighscoreManager.SaveScore(finalScore);
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