using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    public GameObject[] powerUps; // Array mit Prefabs (Health, FireRate, ScoreBoost)
    public float spawnInterval = 5f; // Sekunden zwischen Spawns
    public Vector2 spawnAreaMin = new Vector2(-8f, -4f);
    public Vector2 spawnAreaMax = new Vector2(8f, 4f);

    private float timer;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnPowerUp();
            timer = spawnInterval;
        }
    }

    void SpawnPowerUp()
    {
        if (powerUps.Length == 0) return;

        // Zufällige Position
        Vector2 spawnPos = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        // Zufälliges PowerUp wählen
        int index = Random.Range(0, powerUps.Length);
        Instantiate(powerUps[index], spawnPos, Quaternion.identity);

        Debug.Log($"[PowerUpSpawner] Spawned {powerUps[index].name} at {spawnPos}");
    }
}
