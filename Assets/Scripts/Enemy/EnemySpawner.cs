using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
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

    [Header("Spawnpunkte (empfohlen: Transforms in der Szene)")]
    [Tooltip("Wenn gesetzt, werden diese Transforms als Spawnpunkte verwendet.")]
    public Transform[] spawnPoints;

    [Tooltip("Alternativ: feste Positionen. Entweder World- oder Local-Space.")]
    public Vector3[] spawnPositions;

    [Tooltip("Wenn true, werden spawnPositions relativ zu diesem Spawner interpretiert.")]
    public bool positionsAreLocal = false;

    [Header("Spawn Wahrscheinlichkeiten (Summe = 1.0)")]
    [Range(0f, 1f)] public float fastEnemyChance = 0.6f;
    [Range(0f, 1f)] public float tankEnemyChance = 0.2f;
    [Range(0f, 1f)] public float rangedEnemyChance = 0.2f;

    float timer;

    void Start()
    {
        timer = spawnInterval;
        NormalizeChances();
    }

    void Update()
    {
        if (!playerHealth)
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
        if (maxEnemies > 0)
        {
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (enemyCount >= maxEnemies) return;
        }

        Vector3 spawnPos;
        Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (TryGetSpawnPosition(out spawnPos))
        {
            GameObject prefab = PickPrefab();
            var go = Instantiate(prefab, spawnPos, spawnRot);
            Debug.Log($"[EnemySpawner] Spawned {prefab.name} at {spawnPos} (scene {go.scene.name})");
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] Keine gültigen Spawnpunkte gefunden!");
        }
    }

    bool TryGetSpawnPosition(out Vector3 pos)
    {
        // 1) Szene-Transforms
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var t = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (t)
            {
                pos = t.position;
                return true;
            }
        }

        // 2) Feste Positionen
        if (spawnPositions != null && spawnPositions.Length > 0)
        {
            var p = spawnPositions[Random.Range(0, spawnPositions.Length)];
            pos = positionsAreLocal ? transform.TransformPoint(p) : p;
            return true;
        }

        pos = default;
        return false;
    }

    GameObject PickPrefab()
    {
        float total = fastEnemyChance + tankEnemyChance + rangedEnemyChance;
        float r = Random.value * Mathf.Max(total, 0.0001f);

        if (r < fastEnemyChance) return fastEnemyPrefab;
        r -= fastEnemyChance;
        if (r < tankEnemyChance) return tankEnemyPrefab;
        return rangedEnemyPrefab;
    }

    void NormalizeChances()
    {
        float total = fastEnemyChance + tankEnemyChance + rangedEnemyChance;
        if (total <= 0f) { fastEnemyChance = 1f; tankEnemyChance = rangedEnemyChance = 0f; return; }
        fastEnemyChance /= total;
        tankEnemyChance /= total;
        rangedEnemyChance /= total;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // spawnPoints
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var t in spawnPoints)
            {
                if (!t) continue;
                Gizmos.DrawWireSphere(t.position, 0.4f);
            }
        }

        // spawnPositions
        if (spawnPositions != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var p in spawnPositions)
            {
                Vector3 wp = positionsAreLocal ? transform.TransformPoint(p) : p;
                Gizmos.DrawWireSphere(wp, 0.3f);
            }
        }
    }
#endif
}
