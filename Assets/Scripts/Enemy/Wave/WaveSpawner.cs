using UnityEngine;
using System.Collections;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public ScoreManager scoreManager;
    public SkillManager skillManager;

    [Header("UI Elements")]
    public TextMeshProUGUI centerText;

    [Header("Gates (Spawn Points)")]
    public Transform[] gates;

    [Header("Enemy Prefabs")]
    public GameObject fastEnemyPrefab;
    public GameObject tankEnemyPrefab;
    public GameObject rangedEnemyPrefab;

    [Header("Boss Prefabs")]
    public GameObject boss1Prefab;
    public GameObject boss2Prefab;

    // --- HIER WAREN DIE DOPPELTEN VARIABLEN - JETZT BEREINIGT ---
    [Header("Boss Minion Spawning (Nur für starken Boss)")]
    [Tooltip("Welcher Gegner soll während des Bosskampfes spawnen?")]
    public GameObject bossMinionPrefab; // ZIEH HIER DEN GEGNER REIN (z.B. FastEnemy)

    [Tooltip("Wartezeit bevor der erste Minion kommt (z.B. 2 Sekunden)")]
    public float bossSpawnDelay = 2.0f;

    [Tooltip("Start-Intervall (z.B. 5 Sekunden)")]
    public float bossStartInterval = 5.0f;

    [Tooltip("Ziel-Intervall (z.B. runter auf 2 Sekunden)")]
    public float bossTargetInterval = 2.0f;

    [Tooltip("Wie schnell es schneller wird (0.05 = langsam, 0.2 = schnell)")]
    public float bossDecayRate = 0.05f;

    [Tooltip("Radius um den Boss")]
    public float bossSpawnRadius = 3.0f;
    // ------------------------------------------------------------

    [Header("Waves")]
    public WaveDefinition[] waves;

    [Header("Runtime")]
    public bool autoStart = true;
    public float fastKillTimeLimit = 5.0f;

    private int currentWaveIndex = 0;
    private float lastSpawnTime;

    void Start()
    {
        if (centerText) centerText.text = "";

        if (autoStart)
            StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        // Start Cooldown
        yield return StartCoroutine(StartCountdown(5));

        while (currentWaveIndex < waves.Length)
        {
            if (!playerHealth || playerHealth.currentHealth <= 0)
                yield break;

            WaveDefinition wave = waves[currentWaveIndex];

            // --- WAVE NAME ---
            centerText.text = wave.waveName;
            centerText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
            centerText.text = "";

            // --- SPAWNING ---

            // A) Intro
            if (wave.intro != null)
            {
                foreach (var intro in wave.intro)
                {
                    SpawnEnemy(intro.type, intro.gateIndex);
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }

            // B) Segments
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

            // C) Boss
            if (wave.spawnBoss)
            {
                SpawnBoss(wave.bossVariant);
            }

            // --- WARTEN BIS ALLE TOT SIND ---
            yield return new WaitUntil(() => CountAliveEnemies() == 0);

            // --- SCORE BERECHNEN ---
            yield return StartCoroutine(HandleWaveScore(currentWaveIndex));

            // --- SKILL SELECTION TRIGGER ---
            if (currentWaveIndex == 0 || currentWaveIndex == 1)
            {
                if (skillManager != null)
                {
                    yield return StartCoroutine(skillManager.StartSkillSelectionRoutine());
                }
                else
                {
                    Debug.LogWarning("[WaveSpawner] SkillManager nicht zugewiesen!");
                }
            }

            // --- COOLDOWN ZUR NÄCHSTEN WAVE ---
            if (currentWaveIndex < waves.Length - 1)
            {
                yield return StartCoroutine(StartCountdown(5));
            }

            currentWaveIndex++;
        }
    }

    // ----------------------------------------------------------------------
    // BOSS MINION SPAWNING LOGIK
    // ----------------------------------------------------------------------
    IEnumerator BossMinionSpawnerRoutine(GameObject boss)
    {
        // 1. Warten vor dem allerersten Spawn (z.B. 2 Sekunden)
        yield return new WaitForSeconds(bossSpawnDelay);

        Debug.Log("Boss Minion Spawner gestartet!");

        // Zeit merken, wann der Algorithmus beginnt
        float startTime = Time.time;

        // Schleife läuft, solange der Boss existiert (boss != null)
        while (boss != null)
        {
            if (bossMinionPrefab == null)
            {
                Debug.LogWarning("Kein Minion Prefab zugewiesen!");
                break;
            }

            // 2. SOFORT Spawnen
            SpawnMinionAroundBoss(boss);

            // 3. Berechnen, wie lange wir bis zum NÄCHSTEN Spawn warten
            float timeRunning = Time.time - startTime;

            // Formel: StartInterval nähert sich TargetInterval an
            float currentInterval = bossTargetInterval + (bossStartInterval - bossTargetInterval) * Mathf.Exp(-bossDecayRate * timeRunning);

            // Optional: Debugging
            // Debug.Log($"Nächster Gegner in {currentInterval:F2} Sekunden");

            // 4. Warten basierend auf der Berechnung
            yield return new WaitForSeconds(currentInterval);
        }
    }

    void SpawnMinionAroundBoss(GameObject boss)
    {
        if (boss == null) return;

        // Zufällige Position im Kreis um den Boss
        Vector2 randomCircle = Random.insideUnitCircle.normalized * bossSpawnRadius;

        // Position berechnen relative zum Boss
        Vector3 spawnPos = boss.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // WICHTIG: Die Y-Höhe (Bodenhöhe) vom Boss übernehmen
        spawnPos.y = boss.transform.position.y;

        Instantiate(bossMinionPrefab, spawnPos, Quaternion.identity);
    }
    // ----------------------------------------------------------------------

    IEnumerator HandleWaveScore(int index)
    {
        // --- STANDARD SCORE ---
        int baseReward = (index + 1) * 100;
        int finalReward = baseReward;

        float timeDiff = Time.time - lastSpawnTime;
        bool isFastKill = timeDiff <= fastKillTimeLimit;

        centerText.gameObject.SetActive(true);
        centerText.text = $"Wave Cleared!\n<size=80><color=yellow>+{baseReward}</color></size>";

        yield return new WaitForSeconds(1.5f);

        // Fast Kill
        if (isFastKill)
        {
            centerText.text = $"Wave Cleared!\n<size=80><color=yellow>+{baseReward}</color></size>\n<size=60><color=red>FAST KILL! x2</color></size>";
            yield return new WaitForSeconds(1.0f);
            finalReward *= 2;
            centerText.text = $"<size=60><color=green>+{finalReward}</color></size>";
        }
        else
        {
            centerText.text = $"<size=60><color=white>+{finalReward}</color></size>";
        }

        yield return new WaitForSeconds(1.0f);

        // --- SPEZIAL-LOGIK (WAVE 4, 5, 9) ---
        bool isSpecialWave = (index == 3 || index == 4 || index == 8);

        if (isSpecialWave && playerHealth != null)
        {
            // Full Life Bonus
            if (playerHealth.currentHealth >= playerHealth.maxHealth)
            {
                int hpBonus = 500;
                finalReward += hpBonus;
                centerText.text += $"\n<size=50><color=cyan>PERFECT CONDITION! +{hpBonus}</color></size>";
                yield return new WaitForSeconds(1.5f);
                centerText.text = $"<size=70><color=green>Total: +{finalReward}</color></size>";
            }
            // Health Refill
            else
            {
                while (playerHealth.currentHealth < playerHealth.maxHealth)
                {
                    centerText.text = $"<size=50><color=white>Restoring Health...</color></size>";
                    playerHealth.Heal(1);
                    yield return new WaitForSeconds(1.6f);
                }
                centerText.text = $"<size=60><color=white>+{finalReward}</color></size>";
            }
        }

        if (scoreManager != null) scoreManager.AddScore(finalReward);

        yield return new WaitForSeconds(1.5f);
        centerText.text = "";
        centerText.gameObject.SetActive(false);
    }

    IEnumerator StartCountdown(int seconds)
    {
        centerText.gameObject.SetActive(true);
        for (int i = seconds; i > 0; i--)
        {
            centerText.text = "Next Wave in: <color=orange>" + i.ToString() + "</color>";
            yield return new WaitForSeconds(1f);
        }
        centerText.text = "";
    }

    void SpawnEnemy(EnemyType type, int gateIndex)
    {
        if (gates == null || gates.Length == 0) return;
        if (gateIndex < 0 || gateIndex >= gates.Length) return;

        GameObject prefab = GetPrefab(type);
        if (!prefab) return;

        Vector3 pos = gates[gateIndex].position;
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Instantiate(prefab, pos, rot);

        lastSpawnTime = Time.time;
    }

    void SpawnBoss(BossVariant variant)
    {
        GameObject prefab = (variant == BossVariant.Boss2 && boss2Prefab != null) ? boss2Prefab : boss1Prefab;
        if (!prefab) return;

        float minX = 1f; float maxX = 24f;
        float minZ = -10f; float maxZ = 12f;
        float padding = 2.0f;

        Vector3 spawnPos;

        if (playerHealth != null)
        {
            Vector3 playerPos = playerHealth.transform.position;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(minX + padding, 0.3f, minZ + padding),
                new Vector3(maxX - padding, 0.3f, minZ + padding),
                new Vector3(minX + padding, 0.3f, maxZ - padding),
                new Vector3(maxX - padding, 0.3f, maxZ - padding)
            };

            spawnPos = corners[0];
            float maxDistance = Vector3.Distance(playerPos, corners[0]);

            for (int i = 1; i < corners.Length; i++)
            {
                float dist = Vector3.Distance(playerPos, corners[i]);
                if (dist > maxDistance)
                {
                    maxDistance = dist;
                    spawnPos = corners[i];
                }
            }
        }
        else
        {
            spawnPos = new Vector3(maxX - padding, 0.3f, maxZ - padding);
        }

        // BOSS SPAWNEN
        GameObject bossObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        lastSpawnTime = Time.time;

        BossBeetle beetle = bossObj.GetComponent<BossBeetle>();
        if (beetle != null)
        {
            if (variant == BossVariant.Boss1)
            {
                // SCHWACHER BOSS: Nur Stats
                beetle.ConfigureStats(20, true);
            }
            else
            {
                // STARKER BOSS: Stats + Minions
                beetle.ConfigureStats(30, false);

                // Wir übergeben den Boss direkt an die Coroutine
                StartCoroutine(BossMinionSpawnerRoutine(bossObj));
            }
        }
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