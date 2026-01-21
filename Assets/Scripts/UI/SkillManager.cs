using UnityEngine;
using System.Collections;

public class SkillManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject skillMenuUI;

    [Header("Game State Manager")]
    public GameStateManager gameStateManager; // ZIEH DEN MANAGER HIER REIN!

    private bool isSelecting = false;

    void Start()
    {
        // Falls vergessen, suchen wir ihn selbst
        if (gameStateManager == null) gameStateManager = GameStateManager.Instance;

        if (skillMenuUI) skillMenuUI.SetActive(false);
    }

    public IEnumerator StartSkillSelectionRoutine()
    {
        if (skillMenuUI == null)
        {
            Debug.LogWarning("[SkillManager] Kein UI zugewiesen!");
            yield break;
        }

        // 1. Spiel einfrieren (ESC Taste deaktivieren = false)
        if (gameStateManager != null)
            gameStateManager.SetGameState(true, false);

        // 2. Menü anzeigen
        skillMenuUI.SetActive(true);
        isSelecting = true;

        // 3. Warten auf Wahl
        yield return new WaitUntil(() => isSelecting == false);

        // 4. Menü ausblenden
        skillMenuUI.SetActive(false);

        // 5. Spiel weiterlaufen lassen (ESC Taste wieder erlauben = true)
        if (gameStateManager != null)
            gameStateManager.SetGameState(false, true);
    }

    public void ConfirmSkillSelection()
    {
        isSelecting = false;
    }
}