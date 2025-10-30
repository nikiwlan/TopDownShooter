using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Referenz zur Spieler-Gesundheit (wird für Spawn-Stop benötigt).")]
    public PlayerHealth playerHealth;

    [Header("Enemy Prefabs")]
    public GameObject fastEnemyPrefab;
    public GameObject tankEnemyPrefab;
    public GameObject rangedEnemyPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Sekunden zwischen den Spawns.")]
    public float spawnInterval = 2f;

    [Tooltip("Maximale Anzahl gleichzeitig aktiver Gegner (0 = unbegrenzt).")]
    public int maxEnemies = 0;

    [Tooltip("Spawnpunkte für Gegner.")]
    public Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(8.914398f, 0f, 32.58257f),
        new Vector3(9.253807f, 0f, -24.72913f)
    };

    [Header("Spawn Wahrscheinlichkeiten (Summe = 1.0)")]
    [Range(0f, 1f)] public float fastEnemyChance = 0.6f;
    [Range(0f, 1f)] public float tankEnemyChance = 0.2f;
    [Range(0f, 1f)] public float rangedEnemyChance = 0.2f;

    private float timer;

    void Start()
    {
        timer = spawnInterval;
    }

    void Update()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("[EnemySpawner] Keine PlayerHealth referenziert!");
            return;
        }

        if (playerHealth.currentHealth <= 0) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            TrySpawnEnemy();
            timer = spawnInterval;
        }
    }

    void TrySpawnEnemy()
    {
        // Falls Spawnlimit aktiv ist
        if (maxEnemies > 0)
        {
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (enemyCount >= maxEnemies)
            {
                Debug.Log("[EnemySpawner] Spawn-Limit erreicht.");
                return;
            }
        }

        SpawnRandomEnemy();
    }

    void SpawnRandomEnemy()
    {
        if (spawnPositions.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] Keine Spawnpositionen gesetzt!");
            return;
        }

        int index = Random.Range(0, spawnPositions.Length);
        Vector3 spawnPos = spawnPositions[index];
        Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        float roll = Random.value;
        GameObject prefabToSpawn = fastEnemyPrefab;

        if (roll < fastEnemyChance)
            prefabToSpawn = fastEnemyPrefab;
        else if (roll < fastEnemyChance + tankEnemyChance)
            prefabToSpawn = tankEnemyPrefab;
        else
            prefabToSpawn = rangedEnemyPrefab;

        Instantiate(prefabToSpawn, spawnPos, spawnRot);
        Debug.Log($"[EnemySpawner] {prefabToSpawn.name} gespawnt bei {spawnPos}");
    }
}
