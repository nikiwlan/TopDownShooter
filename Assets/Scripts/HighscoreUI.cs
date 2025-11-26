using UnityEngine;
using TMPro;

public class HighscoreUI : MonoBehaviour
{
    public TextMeshProUGUI HighscoreLeft;
    public TextMeshProUGUI HighscoreRight;

    void Start()
    {
        LoadAndDisplayScores();
    }

    void LoadAndDisplayScores()
    {
        var data = HighscoreManager.LoadScores();

        HighscoreLeft.text = "";
        HighscoreRight.text = "";

        for (int i = 0; i < data.scores.Count; i++)
        {
            string colorTagStart = "";
            string colorTagEnd = "";

            // ⭐ Farben je nach Platzierung ⭐
            switch (i)
            {
                case 0: // Platz 1
                    colorTagStart = "<color=#FFB000>";   // Gold-Orange
                    break;

                case 1: // Platz 2
                    colorTagStart = "<color=#FF7300>";   // Orange-Rot
                    break;

                case 2: // Platz 3
                    colorTagStart = "<color=#FF3C00>";   // warmer Rotton
                    break;
            }

            if (colorTagStart != "")
                colorTagEnd = "</color>";

            string entry = $"{colorTagStart}{i + 1}.  {data.scores[i]}{colorTagEnd}";

            if (i < 5)
                HighscoreLeft.text += entry + "\n";
            else
                HighscoreRight.text += entry + "\n";
        }
    }
}
