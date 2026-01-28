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
        public string skillName;       // z.B. "Critical Hit"
        [TextArea] public string description; // z.B. "15% Chance for 3 DMG"
        public SkillType type;
    }

    // HIER IST DEINE NEUE LISTE:
    public enum SkillType
    {
        FireRateUp,
        Heal,
        SpeedUp,

        // --- NEUE SKILLS ---
        CritUp,         // 15% Chance auf 3 Schaden
        RangeUp,        // +20% Reichweite
        DodgeUp,        // 10% Ausweich-Chance
        FreezeUp,       // 10% Chance Gegner einzufrieren
        VampireUp,      // 5% Chance auf 1 HP bei Kill
        ExplosiveUp,    // Gegner explodieren
        ThornsUp        // 5% Chance Schaden zurückzuwerfen
    }

    [Header("UI References")]
    public GameObject skillMenuUI;

    [Header("Option 1 (Links)")]
    public Button buttonOption1;
    public TextMeshProUGUI textOption1;

    [Header("Option 2 (Rechts)")]
    public Button buttonOption2;
    public TextMeshProUGUI textOption2;

    [Header("Game State & Player")]
    public GameStateManager gameStateManager;
    public PlayerShooting playerShooting;
    public PlayerHealth playerHealth;
    public PlayerMovement playerMovement;

    [Header("Config")]
    public List<SkillData> availableSkills;

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

        SkillData skill1 = availableSkills[Random.Range(0, availableSkills.Count)];
        SkillData skill2 = availableSkills[Random.Range(0, availableSkills.Count)];

        // --- OPTION 1 ---
        if (textOption1)
            textOption1.text = $"{skill1.skillName}\n<size=70%>{skill1.description}";

        buttonOption1.onClick.RemoveAllListeners();
        buttonOption1.onClick.AddListener(() => OnSkillSelected(skill1));

        // --- OPTION 2 ---
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

    // --- HIER IST DIE LOGIK ---
    void ApplySkill(SkillType type)
    {
        Debug.Log($"Wende Skill an: {type}"); // Test-Ausgabe

        switch (type)
        {
            // --- BASICS ---
            case SkillType.FireRateUp:
                if (playerShooting != null) playerShooting.UpgradeFireRate();
                break;

            case SkillType.Heal:
                if (playerHealth != null) playerHealth.Heal(1);
                break;

            case SkillType.SpeedUp:
                if (playerMovement != null) playerMovement.UpgradeSpeed(0.15f);
                break;

            // --- OFFENSIVE (PlayerShooting) ---

            case SkillType.CritUp:
                // Ziel: 15% Chance auf 3 Schaden
                if (playerShooting != null)
                {
                    // playerShooting.UpgradeCritChance(0.15f); 
                    Debug.Log("Crit Chance erhöht!");
                }
                break;

            case SkillType.RangeUp:
                // Ziel: +20% Reichweite
                if (playerShooting != null)
                {
                    // playerShooting.UpgradeRange(1.2f);
                    Debug.Log("Range erhöht!");
                }
                break;

            case SkillType.FreezeUp:
                // Ziel: 10% Chance auf Freeze
                if (playerShooting != null)
                {
                    // playerShooting.UpgradeFreezeChance(0.1f);
                    Debug.Log("Freeze Chance erhöht!");
                }
                break;

            case SkillType.ExplosiveUp:
                // Ziel: Explosion bei Treffer
                if (playerShooting != null)
                {
                    // playerShooting.EnableExplosiveAmmo();
                    Debug.Log("Explosive Ammo aktiviert!");
                }
                break;

            case SkillType.VampireUp:
                // Ziel: 5% Chance auf Heal bei Kill
                // Das muss meistens ins Shooting Skript (weil die Kugel tötet)
                if (playerShooting != null)
                {
                    // playerShooting.UpgradeVampirism(0.05f);
                    Debug.Log("Vampirismus erhöht!");
                }
                break;

            // --- DEFENSIVE (PlayerHealth) ---

            case SkillType.DodgeUp:
                // Ziel: 10% Chance Schaden zu ignorieren
                if (playerHealth != null)
                {
                    // playerHealth.UpgradeDodge(0.1f);
                    Debug.Log("Dodge Chance erhöht!");
                }
                break;

            case SkillType.ThornsUp:
                // Ziel: 5% Chance Schaden zurückzuwerfen
                if (playerHealth != null)
                {
                    // playerHealth.UpgradeThorns(0.05f);
                    Debug.Log("Thorns aktiviert!");
                }
                break;
        }
    }
}