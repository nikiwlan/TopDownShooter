using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Power-Up Einstellungen")]
    public GameObject[] powerUps;
    public float spawnInterval = 5f;
    public float spawnRadius = 6f;
    public Transform player;

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
        if (powerUps.Length == 0 || player == null) return;

        Vector3 spawnPos;
        int maxAttempts = 10;
        bool foundSpot = false;

        // Versuche 10x einen freien Platz zu finden
        for (int i = 0; i < maxAttempts; i++)
        {
            // 🔄 Zufälliger Punkt im Kreis (XZ-Ebene!)
            Vector2 randomOffset2D = Random.insideUnitCircle * spawnRadius;
            spawnPos = new Vector3(player.position.x + randomOffset2D.x, 0f, player.position.z + randomOffset2D.y);

            if (!IsOccupied(spawnPos))
            {
                Instantiate(powerUps[Random.Range(0, powerUps.Length)], spawnPos, Quaternion.identity);
                Debug.Log("[PowerUpSpawner] Spawned PowerUp at " + spawnPos);
                foundSpot = true;
                break;
            }
        }

        if (!foundSpot)
        {
            Debug.LogWarning("[PowerUpSpawner] No free spawn spot found!");
        }
    }

    bool IsOccupied(Vector3 position)
    {
        // Prüft, ob in der Nähe (Radius 1.2) schon ein anderes PowerUp ist
        Collider[] colliders = Physics.OverlapSphere(position, 1.2f);
        foreach (var col in colliders)
        {
            if (col.CompareTag("PowerUp"))
            {
                Debug.Log("[PowerUpSpawner] Platz bei " + position + " ist belegt durch: " + col.name);
                return true;
            }
        }
        return false;
    }
}
