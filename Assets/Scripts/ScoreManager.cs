using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-100)]  // <-- sorgt dafür, dass Instance früh gesetzt wird
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public TextMeshProUGUI scoreText;

    private int score = 0;

    void Awake()
    {
        Instance = this;
        UpdateUI();
        Debug.Log("[ScoreManager] Awake – Instance gesetzt.");
    }

    public void AddScore(int points)
    {
        score += points;
        Debug.Log($"[ScoreManager] AddScore({points}) -> Neuer Score: {score}");
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
        if (scoreText != null) scoreText.text = "Score: " + score;
        else Debug.LogWarning("[ScoreManager] scoreText ist nicht zugewiesen!");
    }
}
