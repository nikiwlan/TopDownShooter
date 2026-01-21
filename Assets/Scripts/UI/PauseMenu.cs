using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;

    // Crosshair und Player Referenzen brauchen wir hier nicht mehr, 
    // das macht jetzt der GameStateManager.

    private bool isPaused = false;
    private GameStateManager gameStateManager;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        gameStateManager = GameStateManager.Instance;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuUI.SetActive(false);

        // Spiel läuft weiter, PauseMenu bleibt aktiv (true) für nächstes Mal
        if (gameStateManager)
            gameStateManager.SetGameState(false, true);
    }

    void PauseGame()
    {
        isPaused = true;
        pauseMenuUI.SetActive(true);

        // Spiel stopp, PauseMenu bleibt aktiv (true), damit ESC funktioniert
        if (gameStateManager)
            gameStateManager.SetGameState(true, true);
    }

    public void BackToMenu()
    {
        // Zeit resetten bevor wir laden
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}