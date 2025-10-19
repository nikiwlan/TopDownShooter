using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;       // Gegner-Prefab-Referenz
    public float spawnInterval = 2f;     // Sekunden zwischen Spawns
    public float spawnRadius = 8f;       // Abstand vom Spieler

    private Transform player;
    private float timer;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        timer = spawnInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnEnemy();
            timer = spawnInterval;
        }
    }

    void SpawnEnemy()
    {
        if (player == null) return;

        // Zufällige Richtung rund um den Spieler
        Vector2 spawnDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = player.position + (Vector3)(spawnDir * spawnRadius);

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}
