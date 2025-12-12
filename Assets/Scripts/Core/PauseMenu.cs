using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;
    public GameObject crosshair;

    private bool isPaused = false;

    private PlayerMovement pm;
    private PlayerShooting ps;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        pm = FindObjectOfType<PlayerMovement>();
        ps = FindObjectOfType<PlayerShooting>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
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
        Time.timeScale = 1f;

        // Maus verstecken
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        // Crosshair sichtbar
        if (crosshair != null)
            crosshair.SetActive(true);

        // Player wieder freigeben
        if (pm != null) pm.isFrozen = false;
        if (ps != null) ps.isFrozen = false;

        pauseMenuUI.SetActive(false);
    }

    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        // Maus zeigen
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Crosshair aus
        if (crosshair != null)
            crosshair.SetActive(false);

        // Player einfrieren (wie beim GameOver)
        if (pm != null) pm.isFrozen = true;
        if (ps != null) ps.isFrozen = true;

        pauseMenuUI.SetActive(true);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
