using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
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

        Vector2 spawnPos;
        int maxAttempts = 10;
        bool foundSpot = false;

        // Versuche 10x einen Platz zu finden, der frei ist
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            spawnPos = (Vector2)player.position + randomOffset;

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

    bool IsOccupied(Vector2 position)
    {
        // Prüft, ob in der Nähe (Radius 1.5) schon ein anderes PowerUp ist
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 1.2f);
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
