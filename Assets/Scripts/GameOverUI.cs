using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Crosshair")]
    public GameObject crosshair;

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
        {
            survivedTime += Time.deltaTime;
        }

        if (!isGameOver && playerHealth.currentHealth <= 0)
        {
            ShowGameOver();
        }
    }


    void ShowGameOver()
    {
        isGameOver = true;

        // Maus einschalten
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // --- Crosshair deaktivieren ---
        if (crosshair != null)
            crosshair.SetActive(false);


        isGameOver = true;

        // Spieler kontrollen einfrieren
        var pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) pm.isFrozen = true;

        var ps = FindObjectOfType<PlayerShooting>();
        if (ps != null) ps.isFrozen = true;

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

        // Zeit berechnen
        int minutes = Mathf.FloorToInt(survivedTime / 60f);
        int seconds = Mathf.FloorToInt(survivedTime % 60f);
        survivedText.text = $"You survived: {minutes:00}:{seconds:00}";

        // Score anzeigen
        int finalScore = ScoreManager.Instance.GetScore();
        gameOverScoreText.text = $"Score: {finalScore}";

        // ⭐⭐⭐ HIER SPEICHERN WIR DEN SCORE ⭐⭐⭐
        HighscoreManager.SaveScore(finalScore);

        // Spiel einfrieren
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
