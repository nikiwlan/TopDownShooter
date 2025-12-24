using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("Gates (Spawn Points)")]
    public Transform[] gates;

    [Header("Enemy Prefabs")]
    public GameObject fastEnemyPrefab;
    public GameObject tankEnemyPrefab;
    public GameObject rangedEnemyPrefab;

    [Header("Boss Prefabs")]
    public GameObject boss1Prefab;   // Wave 10
    public GameObject boss2Prefab;   // Wave 20

    [Header("Waves (ScriptableObjects)")]
    public WaveDefinition[] waves;

    [Header("Runtime")]
    public bool autoStart = true;

    private int currentWaveIndex = 0;

    void Start()
    {
        if (autoStart)
            StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        while (currentWaveIndex < waves.Length)
        {
            if (!playerHealth || playerHealth.currentHealth <= 0)
                yield break;

            WaveDefinition wave = waves[currentWaveIndex];
            Debug.Log($"[WaveSpawner] START {wave.waveName}");

            // 1) Intro (scripted)
            if (wave.intro != null)
            {
                foreach (var intro in wave.intro)
                {
                    SpawnEnemy(intro.type, intro.gateIndex);
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }

            // 2) Segments (random but controlled)
            if (wave.segments != null)
            {
                foreach (var segment in wave.segments)
                {
                    for (int i = 0; i < segment.count; i++)
                    {
                        if (segment.activeGates == null || segment.activeGates.Length == 0) break;
                        if (segment.pool == null || segment.pool.Length == 0) break;

                        int gate = segment.activeGates[Random.Range(0, segment.activeGates.Length)];
                        EnemyType type = PickWeighted(segment.pool);

                        SpawnEnemy(type, gate);
                        yield return new WaitForSeconds(wave.spawnInterval);
                    }
                }
            }

            // 3) Boss
            if (wave.spawnBoss)
            {
                SpawnBoss(wave.bossVariant, wave.bossGateIndex);
            }

            // 4) Wait until wave is cleared
            yield return new WaitUntil(() => CountAliveEnemies() == 0);

            Debug.Log($"[WaveSpawner] END {wave.waveName}");
            currentWaveIndex++;
        }

        Debug.Log("[WaveSpawner] ALL WAVES COMPLETED – GAME WON");
    }

    // ---------- SPAWN ----------

    void SpawnEnemy(EnemyType type, int gateIndex)
    {
        if (gates == null || gates.Length == 0) return;
        if (gateIndex < 0 || gateIndex >= gates.Length) return;

        GameObject prefab = GetPrefab(type);
        if (!prefab) return;

        Vector3 pos = gates[gateIndex].position;
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Instantiate(prefab, pos, rot);
    }

    void SpawnBoss(BossVariant variant, int gateIndex)
    {
        if (gates == null || gates.Length == 0) return;
        if (gateIndex < 0 || gateIndex >= gates.Length) return;

        GameObject prefab = (variant == BossVariant.Boss2) ? boss2Prefab : boss1Prefab;
        if (!prefab) return;

        Vector3 pos = gates[gateIndex].position;
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Instantiate(prefab, pos, rot);

        Debug.Log($"[WaveSpawner] Spawned BOSS: {variant} at Gate {gateIndex}");
    }

    GameObject GetPrefab(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Fast: return fastEnemyPrefab;
            case EnemyType.Tank: return tankEnemyPrefab;
            case EnemyType.Ranged: return rangedEnemyPrefab;
        }
        return null;
    }

    EnemyType PickWeighted(WeightedEnemy[] pool)
    {
        float total = 0f;
        for (int i = 0; i < pool.Length; i++) total += pool[i].weight;

        float r = Random.value * Mathf.Max(total, 0.0001f);
        for (int i = 0; i < pool.Length; i++)
        {
            r -= pool[i].weight;
            if (r <= 0f) return pool[i].type;
        }
        return pool[pool.Length - 1].type;
    }

    int CountAliveEnemies()
    {
        return GameObject.FindGameObjectsWithTag("Enemy").Length;
    }
}