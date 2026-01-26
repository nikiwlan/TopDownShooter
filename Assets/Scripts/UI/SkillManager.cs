using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    [System.Serializable]
    public class SkillData
    {
        public string skillName;       // z.B. "Rapid Fire"
        [TextArea] public string description; // z.B. "+10% Fire Rate"
        public SkillType type;
    }

    public enum SkillType { FireRateUp, Heal, SpeedUp }

    [Header("UI References")]
    public GameObject skillMenuUI;

    [Header("Option 1 (Links)")]
    public Button buttonOption1;
    public TextMeshProUGUI textOption1; // Das EINE Textfeld der linken Karte

    [Header("Option 2 (Rechts)")]
    public Button buttonOption2;
    public TextMeshProUGUI textOption2; // Das EINE Textfeld der rechten Karte

    [Header("Game State & Player")]
    public GameStateManager gameStateManager;
    public PlayerShooting playerShooting;
    public PlayerHealth playerHealth;

    [Header("Config")]
    public List<SkillData> availableSkills; // Deine Liste mit Skills

    private bool isSelecting = false;

    void Start()
    {
        if (gameStateManager == null) gameStateManager = GameStateManager.Instance;
        if (skillMenuUI) skillMenuUI.SetActive(false);
    }

    public IEnumerator StartSkillSelectionRoutine()
    {
        if (skillMenuUI == null) yield break;

        // 1. Pause
        if (gameStateManager != null) gameStateManager.SetGameState(true, false);

        // 2. Texte setzen & Buttons vorbereiten
        PrepareSkillOptions();

        // 3. Menü an
        skillMenuUI.SetActive(true);
        isSelecting = true;

        // 4. Warten auf Klick
        yield return new WaitUntil(() => isSelecting == false);

        // 5. Menü aus & Weiter
        skillMenuUI.SetActive(false);
        if (gameStateManager != null) gameStateManager.SetGameState(false, true);
    }

    void PrepareSkillOptions()
    {
        if (availableSkills.Count == 0) return;

        // ZUFALLSWAHL
        SkillData skill1 = availableSkills[Random.Range(0, availableSkills.Count)];
        SkillData skill2 = availableSkills[Random.Range(0, availableSkills.Count)];

        // --- OPTION 1 (LINKS) ---
        // Wir schreiben Titel und Beschreibung in EIN Feld
        // <size=70%> macht die Beschreibung etwas kleiner, sieht schicker aus!
        if (textOption1)
            textOption1.text = $"{skill1.skillName}\n<size=70%>{skill1.description}";

        buttonOption1.onClick.RemoveAllListeners();
        buttonOption1.onClick.AddListener(() => OnSkillSelected(skill1));

        // --- OPTION 2 (RECHTS) ---
        if (textOption2)
            textOption2.text = $"{skill2.skillName}\n<size=70%>{skill2.description}";

        buttonOption2.onClick.RemoveAllListeners();
        buttonOption2.onClick.AddListener(() => OnSkillSelected(skill2));
    }

    void OnSkillSelected(SkillData selectedSkill)
    {
        ApplySkill(selectedSkill.type);
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
                if (playerHealth != null) playerHealth.Heal(1);
                break;

                // Weitere Cases hier...
        }
    }
}