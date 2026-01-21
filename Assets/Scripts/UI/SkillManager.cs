using UnityEngine;
using System.Collections;

public class SkillManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject skillMenuUI;

    [Header("Game Control References")]
    [Tooltip("Zieh hier dein Crosshair-GameObject rein")]
    public GameObject crosshairUI;

    [Tooltip("Zieh hier das Skript rein, das den Spieler bewegt/dreht (z.B. PlayerController)")]
    public MonoBehaviour playerMovementScript;

    [Tooltip("Zieh hier das Skript rein, das schieﬂt (z.B. ShootingController)")]
    public MonoBehaviour playerShootingScript;

    [Tooltip("Zieh hier dein PauseMenu-Skript rein")]
    public MonoBehaviour pauseMenuScript;

    private bool isSelecting = false;

    void Start()
    {
        if (skillMenuUI) skillMenuUI.SetActive(false);
    }

    public IEnumerator StartSkillSelectionRoutine()
    {
        if (skillMenuUI == null)
        {
            Debug.LogWarning("[SkillManager] Kein UI zugewiesen!");
            yield break;
        }

        // 1. Alles DEAKTIVIEREN
        SetGameControlsActive(false);

        // 2. Men¸ anzeigen & Zeit stopp
        skillMenuUI.SetActive(true);
        isSelecting = true;
        Time.timeScale = 0f;

        // 3. Warten auf Wahl
        yield return new WaitUntil(() => isSelecting == false);

        // 4. Aufr‰umen & Zeit weiter
        Time.timeScale = 1f;
        skillMenuUI.SetActive(false);

        // 5. Alles wieder AKTIVIEREN
        SetGameControlsActive(true);
    }

    public void ConfirmSkillSelection()
    {
        isSelecting = false;
    }

    // Hilfsfunktion zum An/Ausschalten
    void SetGameControlsActive(bool isActive)
    {
        // Crosshair ausblenden/einblenden
        if (crosshairUI) crosshairUI.SetActive(isActive);

        // Bewegung/Drehung sperren/entsperren
        if (playerMovementScript) playerMovementScript.enabled = isActive;

        // Schieﬂen sperren/entsperren
        if (playerShootingScript) playerShootingScript.enabled = isActive;

        // Pause-Taste (ESC) sperren/entsperren
        if (pauseMenuScript) pauseMenuScript.enabled = isActive;
    }
}