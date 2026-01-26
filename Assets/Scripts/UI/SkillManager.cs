using UnityEngine;
using UnityEngine.UI;
using TMPro; // Falls du TextMeshPro nutzt (empfohlen!), sonst nimm 'using UnityEngine.UI;' für normale Text
using System.Collections;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    // Kleine Klasse für Skill-Daten
    [System.Serializable]
    public class SkillData
    {
        public string skillName;
        public string description;
        public SkillType type;
    }

    public enum SkillType { FireRateUp, Heal, SpeedUp } // Hier kannst du später erweitern

    [Header("UI References")]
    public GameObject skillMenuUI;

    // Referenzen zu den Buttons im UI
    public Button buttonOption1;
    public TextMeshProUGUI textOption1; // Oder 'Text', falls du Legacy Text nutzt

    public Button buttonOption2;
    public TextMeshProUGUI textOption2;

    [Header("Game State & Player")]
    public GameStateManager gameStateManager;
    public PlayerShooting playerShooting; // ZIEH DEN SPIELER HIER REIN!

    [Header("Config")]
    public List<SkillData> availableSkills; // Füll das im Inspector!

    private bool isSelecting = false;
    private SkillData currentSkillOption1;
    private SkillData currentSkillOption2;

    void Start()
    {
        if (gameStateManager == null) gameStateManager = GameStateManager.Instance;
        if (skillMenuUI) skillMenuUI.SetActive(false);

        // Buttons vorbereiten (Listener hinzufügen)
        buttonOption1.onClick.AddListener(() => OnSkillSelected(currentSkillOption1));
        buttonOption2.onClick.AddListener(() => OnSkillSelected(currentSkillOption2));
    }

    public IEnumerator StartSkillSelectionRoutine()
    {
        if (skillMenuUI == null) yield break;

        // 1. Pause
        if (gameStateManager != null) gameStateManager.SetGameState(true, false);

        // 2. Zufällige Skills auswählen
        PrepareSkillOptions();

        // 3. Menü an
        skillMenuUI.SetActive(true);
        isSelecting = true;

        // 4. Warten bis Button geklickt wurde
        yield return new WaitUntil(() => isSelecting == false);

        // 5. Menü aus & Weiter
        skillMenuUI.SetActive(false);
        if (gameStateManager != null) gameStateManager.SetGameState(false, true);
    }

    void PrepareSkillOptions()
    {
        // Wähle zufällige Skills aus deiner Liste
        // (Für den Anfang nehmen wir einfach Random aus der Liste)
        if (availableSkills.Count > 0)
        {
            currentSkillOption1 = availableSkills[Random.Range(0, availableSkills.Count)];
            currentSkillOption2 = availableSkills[Random.Range(0, availableSkills.Count)];

            // UI Updaten
            textOption1.text = $"{currentSkillOption1.skillName}\n<size=70%>{currentSkillOption1.description}";
            textOption2.text = $"{currentSkillOption2.skillName}\n<size=70%>{currentSkillOption2.description}";
        }
    }

    // Wird aufgerufen, wenn man einen Button drückt
    void OnSkillSelected(SkillData selectedSkill)
    {
        if (selectedSkill == null) return;

        // Skill anwenden
        ApplySkill(selectedSkill.type);

        // Auswahl beenden -> Routine läuft weiter
        isSelecting = false;
    }

    void ApplySkill(SkillType type)
    {
        switch (type)
        {
            case SkillType.FireRateUp:
                if (playerShooting != null) playerShooting.UpgradeFireRate();
                break;

            case SkillType.Heal:
                // Später: playerHealth.Heal(20);
                Debug.Log("Heilung gewählt (noch nicht implementiert)");
                break;

            case SkillType.SpeedUp:
                Debug.Log("Speed gewählt (noch nicht implementiert)");
                break;
        }
    }
}