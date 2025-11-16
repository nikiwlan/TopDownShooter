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

    private int score = 0;

    [HideInInspector] public int scoreMultiplier = 1;

    private Coroutine boostRoutine;
    private Coroutine delayedUIRoutine;

    // ----------------------------------------------------------
    // INITIALISIERUNG
    // ----------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (scoreText == null)
        {
            scoreText = FindObjectOfType<TextMeshProUGUI>();
        }

        score = startingScore;
        UpdateUIImmediate();
    }

    // ----------------------------------------------------------
    // SCORE VERGEBEN
    // ----------------------------------------------------------
    public void AddScore(int basePoints)
    {
        if (basePoints <= 0) return;

        int finalPoints = basePoints * scoreMultiplier;
        score += finalPoints;

        Debug.Log($"[ScoreManager] AddScore({basePoints}) x{scoreMultiplier} = +{finalPoints}");

        // ✨ Hier NICHT sofort UI updaten → verzögert!
        StartDelayedUIUpdate();
    }

    // ----------------------------------------------------------
    // VERZÖGERTE UI-AKTUALISIERUNG
    // ----------------------------------------------------------
    private void StartDelayedUIUpdate()
    {
        if (delayedUIRoutine != null)
            StopCoroutine(delayedUIRoutine);

        delayedUIRoutine = StartCoroutine(DelayedUI());
    }

    private IEnumerator DelayedUI()
    {
        // Delay länger als Popup-Flug (1.2s) → z. B. 1.35 Sekunden
        yield return new WaitForSeconds(1.35f);

        UpdateUIImmediate();
        delayedUIRoutine = null;
    }

    private void UpdateUIImmediate()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    // ----------------------------------------------------------
    // SCORE BOOST SYSTEM
    // ----------------------------------------------------------
    public void ApplyScoreBoost(float duration)
    {
        if (boostRoutine != null)
        {
            StopCoroutine(boostRoutine);
        }

        boostRoutine = StartCoroutine(TempScoreBoost(duration));
    }

    private IEnumerator TempScoreBoost(float duration)
    {
        scoreMultiplier = 2;
        Debug.Log($"[ScoreManager] 🔥 ScoreBoost aktiviert für {duration} Sekunden.");

        float t = duration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        scoreMultiplier = 1;
        boostRoutine = null;

        Debug.Log("[ScoreManager] ⏳ ScoreBoost abgelaufen.");
    }

    public int GetScore()
    {
        return score;
    }
}
