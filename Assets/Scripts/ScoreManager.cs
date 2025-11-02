using UnityEngine;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Score Settings")]
    [SerializeField] private int startingScore = 0;
    [SerializeField] private int scoreMultiplier = 1;

    private int score = 0;
    private bool scoreBoostActive = false;
    private Coroutine boostRoutine;

    // ----------------------------------------------------------
    // INITIALISIERUNG
    // ----------------------------------------------------------
    void Awake()
    {
        // Singleton-Zuweisung
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Falls im Inspector kein ScoreText gesetzt wurde → automatisch suchen
        if (scoreText == null)
        {
            scoreText = FindObjectOfType<TextMeshProUGUI>();
            if (scoreText != null)
                Debug.Log("[ScoreManager] scoreText automatisch gefunden.");
            else
                Debug.LogWarning("[ScoreManager] Kein ScoreText gefunden – bitte im Inspector zuweisen!");
        }

        // Startwerte setzen
        score = startingScore;
        UpdateUI();

        Debug.Log("[ScoreManager] Awake – Instance aktiv und Score initialisiert.");
    }

    // ----------------------------------------------------------
    // SCORE HANDLING
    // ----------------------------------------------------------
    public void AddScore(int points)
    {
        if (points <= 0) return;

        int finalPoints = points * scoreMultiplier;
        score += finalPoints;

        Debug.Log($"[ScoreManager] AddScore({points}) x{scoreMultiplier} = +{finalPoints} → Neuer Score: {score}");
        UpdateUI();
    }

    public void ResetScore()
    {
        score = startingScore;
        Debug.Log("[ScoreManager] ResetScore() auf " + score);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
        else
        {
            Debug.LogWarning("[ScoreManager] ⚠️ scoreText ist NULL – keine Anzeige möglich!");
        }
    }

    // ----------------------------------------------------------
    // SCORE BOOST SYSTEM
    // ----------------------------------------------------------
    public void ApplyScoreBoost(float duration)
    {
        if (boostRoutine != null)
        {
            StopCoroutine(boostRoutine);
            Debug.Log("[ScoreManager] Vorheriger ScoreBoost überschrieben.");
        }

        boostRoutine = StartCoroutine(TempScoreBoost(duration));
    }

    private IEnumerator TempScoreBoost(float duration)
    {
        scoreBoostActive = true;
        scoreMultiplier = 2; // doppelte Punkte
        Debug.Log($"[ScoreManager] 🔥 ScoreBoost aktiviert für {duration} Sekunden.");

        float timer = duration;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        scoreMultiplier = 1;
        scoreBoostActive = false;
        boostRoutine = null;
        Debug.Log("[ScoreManager] ⏳ ScoreBoost abgelaufen – zurück auf normalen Score.");
    }

    // ----------------------------------------------------------
    // DEBUGGING HILFE
    // ----------------------------------------------------------
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"Score: {score}");
    }
#endif
}
