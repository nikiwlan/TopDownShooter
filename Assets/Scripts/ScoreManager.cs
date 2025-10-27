using UnityEngine;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public TextMeshProUGUI scoreText;

    private int score = 0;

    // --- ScoreBoost Variablen ---
    private bool scoreBoostActive = false;
    private int scoreMultiplier = 1;
    private Coroutine boostRoutine;

    void Awake()
    {
        Instance = this;
        UpdateUI();
        Debug.Log("[ScoreManager] Awake – Instance gesetzt.");
    }

    public void AddScore(int points)
    {
        int finalPoints = points * scoreMultiplier;
        score += finalPoints;
        Debug.Log($"[ScoreManager] AddScore({points}) x{scoreMultiplier} -> Neuer Score: {score}");
        UpdateUI();
    }

    public void ResetScore()
    {
        score = 0;
        Debug.Log("[ScoreManager] ResetScore()");
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
        else
            Debug.LogWarning("[ScoreManager] scoreText ist nicht zugewiesen!");
    }

    // ScoreBoost-Funktion
    public void ApplyScoreBoost(float duration)
    {
        if (boostRoutine != null)
            StopCoroutine(boostRoutine);

        boostRoutine = StartCoroutine(TempScoreBoost(duration));
    }

    private IEnumerator TempScoreBoost(float duration)
    {
        scoreBoostActive = true;
        scoreMultiplier = 2; // doppelte Punkte
        Debug.Log($"[ScoreManager] ScoreBoost aktiviert für {duration} Sekunden.");

        yield return new WaitForSeconds(duration);

        scoreMultiplier = 1;
        scoreBoostActive = false;
        boostRoutine = null;
        Debug.Log("[ScoreManager] ScoreBoost abgelaufen.");
    }
}
