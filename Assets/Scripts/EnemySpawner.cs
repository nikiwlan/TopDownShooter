using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public GameObject enemyPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public float spawnRadius = 10f;           // Abstand um Spieler
    public float minDistanceFromCamera = 0.1f; // wie weit außerhalb des Sichtfelds

    private Transform player;
    private float timer;
    private Camera mainCam;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        mainCam = Camera.main;
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
            {
                Destroy(enemy);
            }

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
        if (player == null || mainCam == null) return;

        int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            // 🔄 Zufällige Richtung im Kreis (XZ-Ebene)
            Vector2 spawnDir2D = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = new Vector3(
                player.position.x + spawnDir2D.x * spawnRadius,
                0f, // 🟢 immer auf Bodenhöhe
                player.position.z + spawnDir2D.y * spawnRadius
            );

            // Kamera-Position in Viewport umwandeln
            Vector3 viewportPos = mainCam.WorldToViewportPoint(spawnPos);

            // Prüfen, ob Punkt außerhalb des Bildschirms liegt
            bool outsideScreen =
                viewportPos.x < -minDistanceFromCamera || viewportPos.x > 1 + minDistanceFromCamera ||
                viewportPos.y < -minDistanceFromCamera || viewportPos.y > 1 + minDistanceFromCamera;

            if (outsideScreen)
            {
                Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                Debug.Log($"[EnemySpawner] Spawned enemy outside view at {spawnPos}");
                return;
            }
        }

        Debug.LogWarning("[EnemySpawner] Kein passender Spawnpunkt außerhalb des Sichtfelds gefunden!");
    }
}
