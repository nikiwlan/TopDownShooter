using UnityEngine;
using TMPro;

public class HighscoreUI : MonoBehaviour
{
    public TextMeshProUGUI HighscoreLeft;   // NEU
    public TextMeshProUGUI HighscoreRight;  // NEU

    void Start()
    {
        LoadAndDisplayScores();
    }

    void LoadAndDisplayScores()
    {
        var data = HighscoreManager.LoadScores();

        // Spalten leeren
        HighscoreLeft.text = "";
        HighscoreRight.text = "";

        for (int i = 0; i < data.scores.Count; i++)
        {
            string entry = $"{i + 1}.  {data.scores[i]}";

            if (i < 5)
                HighscoreLeft.text += entry + "\n";
            else
                HighscoreRight.text += entry + "\n";
        }
    }
}
