using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    // Singleton Pattern für einfachen Zugriff (optional, aber praktisch)
    public static GameStateManager Instance;

    [Header("References")]
    public GameObject crosshair;
    public PauseMenu pauseMenuScript; // Referenz zum Skript, um ESC zu blockieren

    // Wir suchen uns PlayerMovement und Shooting automatisch oder ziehen sie rein
    [HideInInspector] public PlayerMovement playerMovement;
    [HideInInspector] public PlayerShooting playerShooting;

    void Awake()
    {
        // Singleton Setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Automatisch finden, falls nicht im Inspector zugewiesen
        if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerShooting == null) playerShooting = FindObjectOfType<PlayerShooting>();
        if (pauseMenuScript == null) pauseMenuScript = FindObjectOfType<PauseMenu>();
    }

    /// <summary>
    /// Zentraler Schalter für den Spielzustand.
    /// </summary>
    /// <param name="isFrozen">True = Spiel pausiert, Maus sichtbar. False = Spiel läuft.</param>
    /// <param name="allowPauseMenu">Darf man noch ESC drücken? (Bei Skill/GameOver meistens False)</param>
    public void SetGameState(bool isFrozen, bool allowPauseMenu)
    {
        // 1. Zeit steuern
        Time.timeScale = isFrozen ? 0f : 1f;

        // 2. Maus Cursor Logik
        Cursor.visible = isFrozen;
        Cursor.lockState = isFrozen ? CursorLockMode.None : CursorLockMode.Confined;

        // 3. Crosshair steuern
        if (crosshair != null)
            crosshair.SetActive(!isFrozen);

        // 4. Spieler Inputs steuern
        if (playerMovement != null) playerMovement.isFrozen = isFrozen;
        if (playerShooting != null) playerShooting.isFrozen = isFrozen;

        // 5. PAUSE BLOCKIEREN (Der neue Weg)
        // Wenn allowPauseMenu FALSE ist, dann ist IsLocked TRUE.
        PauseMenu.IsLocked = !allowPauseMenu;
    }
}