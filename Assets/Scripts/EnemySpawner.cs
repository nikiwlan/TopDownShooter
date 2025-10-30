using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public GameObject enemyPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 2f;

    // Feste Spawnpunkte (z. B. hinter den Toren)
    private Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(8.914398f, 0f, 32.58257f),   // Eingang Nord
        new Vector3(9.253807f, 0f, -24.72913f)   // Eingang Süd
    };

    private float timer;

    void Start()
    {
        timer = spawnInterval;
    }

    void Update()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("[EnemySpawner] PlayerHealth ist im Spawner nicht gesetzt!");
            return;
        }

        if (playerHealth.currentHealth <= 0)
        {
            Debug.Log("[EnemySpawner] Spieler ist tot – Spawner stoppt und löscht alle Gegner.");
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
                Destroy(enemy);
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            SpawnEnemy();
            timer = spawnInterval;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] Kein Gegnerprefab gesetzt!");
            return;
        }

        // 🔄 Zufällig einen der beiden Spawnpunkte wählen
        int index = Random.Range(0, spawnPositions.Length);
        Vector3 spawnPos = spawnPositions[index];

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[EnemySpawner] Gegner gespawnt bei Tor {index + 1} ({spawnPos})");
    }
}
