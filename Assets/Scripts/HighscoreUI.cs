using UnityEngine;
using TMPro;

public class HighscoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text highscoreText;

    private void Start()
    {
        var data = HighscoreManager.LoadScores();

        string output = "Highscores\n\n";

        for (int i = 0; i < data.scores.Count; i++)
        {
            output += $"{i + 1}.   {data.scores[i]}\n";
        }

        highscoreText.text = output;
    }
}
