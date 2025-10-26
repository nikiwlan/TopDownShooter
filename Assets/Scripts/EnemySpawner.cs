using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public PlayerHealth playerHealth; 
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
        // Prüfe zuerst, ob playerHealth überhaupt existiert
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth ist im Spawner nicht gesetzt!");
            return;
        }

        // Wenn Spieler 0 oder weniger HP hat → Spawner stoppen und alle Gegner zerstören
        if (playerHealth.currentHealth <= 0)
        {
            Debug.Log("Spieler ist tot – Spawner stoppt und löscht alle Gegner.");

            // Alle Gegner finden und zerstören
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                Destroy(enemy);
            }

            // Methode beenden
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
        if (player == null) return;

        // Zufällige Richtung rund um den Spieler
        Vector2 spawnDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = player.position + (Vector3)(spawnDir * spawnRadius);

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}
