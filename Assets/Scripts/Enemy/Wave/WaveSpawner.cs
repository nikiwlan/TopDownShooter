using UnityEngine;
using System.Collections;
using TMPro; // WICHTIG: Für TextMeshPro

public class WaveSpawner : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public ScoreManager scoreManager; // ZIEH DEINEN SCORE MANAGER HIER REIN!

    [Header("UI Elements")]
    public TextMeshProUGUI centerText;  // Dein Text in der Mitte
    public GameObject skillMenuUI;      // Dein Skill-Auswahl Panel

    [Header("Gates (Spawn Points)")]
    public Transform[] gates;

    [Header("Enemy Prefabs")]
    public GameObject fastEnemyPrefab;
    public GameObject tankEnemyPrefab;
    public GameObject rangedEnemyPrefab;

    [Header("Boss Prefabs")]
    public GameObject boss1Prefab;
    public GameObject boss2Prefab;

    [Header("Waves")]
    public WaveDefinition[] waves;

    [Header("Runtime")]
    public bool autoStart = true;
    public float fastKillTimeLimit = 5.0f; // 5 Sekunden Zeitfenster für Bonus

    private int currentWaveIndex = 0;
    private bool skillMenuOpen = false; // Check ob wir warten müssen
    private float lastSpawnTime; // Hier speichern wir den Zeitpunkt des letzten Spawns

    void Start()
    {
        // Sicherstellen, dass UI am Anfang richtig ist
        if (skillMenuUI) skillMenuUI.SetActive(false);
        if (centerText) centerText.text = "";

        if (autoStart)
            StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        // --- 1. START COOLDOWN (5 Sekunden ganz am Anfang) ---
        yield return StartCoroutine(StartCountdown(5));

        while (currentWaveIndex < waves.Length)
        {
            if (!playerHealth || playerHealth.currentHealth <= 0)
                yield break;

            WaveDefinition wave = waves[currentWaveIndex];

            // --- 2. WAVE NAME ANZEIGEN ---
            centerText.text = wave.waveName;
            centerText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
            centerText.text = "";

            Debug.Log($"[WaveSpawner] START {wave.waveName}");

            // --- 3. SPAWNING ---
            // Wir aktualisieren lastSpawnTime bei JEDEM Spawn

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
                SpawnBoss(wave.bossVariant, wave.bossGateIndex);
            }

            // --- 4. WARTEN BIS ALLE TOT SIND ---
            yield return new WaitUntil(() => CountAliveEnemies() == 0);

            Debug.Log($"[WaveSpawner] END {wave.waveName}");

            // --- NEU: SCORE BERECHNUNG & ANZEIGE ---
            // Wir warten hier kurz, um die Score-Animation abzuspielen, bevor Skills kommen
            yield return StartCoroutine(HandleWaveScore(currentWaveIndex));

            // --- 5. SKILL CHECK (Nach Wave 4 und 9) ---
            if (currentWaveIndex == 3 || currentWaveIndex == 8)
            {
                yield return StartCoroutine(HandleSkillSelection());
            }

            // --- 6. COOLDOWN ZUR NÄCHSTEN WAVE (5 Sekunden) ---
            if (currentWaveIndex < waves.Length - 1)
            {
                yield return StartCoroutine(StartCountdown(5));
            }

            currentWaveIndex++;
        }

    }

    // ---------- NEU: SCORE METHODE ----------

    IEnumerator HandleWaveScore(int index)
    {
        // 1. Basis Score berechnen: Wave 1 = 100, Wave 2 = 200...
        int baseReward = (index + 1) * 100;
        int finalReward = baseReward;

        // 2. Zeit prüfen: Wann wurde der letzte Gegner getötet (jetzt) vs. wann ist er gespawnt?
        float timeDiff = Time.time - lastSpawnTime;
        bool isFastKill = timeDiff <= fastKillTimeLimit;

        centerText.gameObject.SetActive(true);

        // SCHRITT A: Basis Punkte anzeigen (Gelb)
        // <size=80> macht es größer
        centerText.text = $"Wave Cleared!\n<size=80><color=yellow>+{baseReward}</color></size>";

        yield return new WaitForSeconds(1.5f); // 1.5 Sekunden warten

        // SCHRITT B: Bonus Check
        if (isFastKill)
        {
            // Zeige den Multiplikator Text (Rot blinkend Vorstellung)
            centerText.text = $"Wave Cleared!\n<size=80><color=yellow>+{baseReward}</color></size>\n<size=60><color=red>FAST KILL! x2</color></size>";

            yield return new WaitForSeconds(1.0f); // Spannung...

            // Verdoppeln
            finalReward *= 2;

            // Endergebnis anzeigen (Grün und Groß)
            centerText.text = $"<size=60><color=green>+{finalReward}</color></size>";
        }
        else
        {
            // Kein Bonus, einfach Score nochmal bestätigen
            centerText.text = $"<size=60><color=white>+{finalReward}</color></size>";
        }

        // SCHRITT C: Score dem Manager geben
        if (scoreManager != null)
        {
            // Passen den Namen 'AddScore' an, falls deine Methode anders heißt!
            scoreManager.AddScore(finalReward);
        }

        yield return new WaitForSeconds(1.5f); // Ergebnis kurz stehen lassen
        centerText.text = ""; // Text weg
    }

    // ---------- HELPER ROUTINES ----------

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

    IEnumerator HandleSkillSelection()
    {
        Debug.Log("Skill Selection Started");

        if (skillMenuUI != null)
        {
            skillMenuUI.SetActive(true);
            skillMenuOpen = true;
            Time.timeScale = 0f;
            yield return new WaitUntil(() => skillMenuOpen == false);
            Time.timeScale = 1f;
            skillMenuUI.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Kein SkillMenuUI zugewiesen!");
        }
    }

    public void OnSkillSelected()
    {
        skillMenuOpen = false;
    }

    // ---------- SPAWN LOGIC (Mit lastSpawnTime Update) ----------

    void SpawnEnemy(EnemyType type, int gateIndex)
    {
        if (gates == null || gates.Length == 0) return;
        if (gateIndex < 0 || gateIndex >= gates.Length) return;

        GameObject prefab = GetPrefab(type);
        if (!prefab) return;

        Vector3 pos = gates[gateIndex].position;
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Instantiate(prefab, pos, rot);

        // WICHTIG: Zeit merken!
        lastSpawnTime = Time.time;
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

        // WICHTIG: Zeit merken!
        lastSpawnTime = Time.time;
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