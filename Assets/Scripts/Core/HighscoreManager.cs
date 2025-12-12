using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class HighscoreData
{
    public List<int> scores = new List<int>();
}

public static class HighscoreManager
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "scores.json");

    public static void SaveScore(int newScore)
    {
        HighscoreData data = LoadScores();
        data.scores.Add(newScore);

        // Sortiere: höchste zuerst
        data.scores.Sort((a, b) => b.CompareTo(a));

        // Nur Top 10 behalten
        if (data.scores.Count > 10)
            data.scores = data.scores.GetRange(0, 10);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);
    }

    public static HighscoreData LoadScores()
    {
        if (!File.Exists(FilePath))
            return new HighscoreData();

        string json = File.ReadAllText(FilePath);
        return JsonUtility.FromJson<HighscoreData>(json);
    }
}
