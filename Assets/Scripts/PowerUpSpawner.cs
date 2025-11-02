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
        if (player == null || powerUps.Length == 0)
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnPowerUp();
            timer = spawnInterval;
        }
    }

    void SpawnPowerUp()
    {
        Vector3 spawnPos;
        int maxAttempts = 10;
        bool foundSpot = false;

        for (int i = 0; i < maxAttempts; i++)
        {
            // Zufälliger Punkt um Spieler (XZ-Ebene)
            Vector2 randomOffset2D = Random.insideUnitCircle * spawnRadius;
            spawnPos = new Vector3(player.position.x + randomOffset2D.x, 0f, player.position.z + randomOffset2D.y);

            if (!IsOccupied(spawnPos))
            {
                GameObject prefab = powerUps[Random.Range(0, powerUps.Length)];
                Instantiate(prefab, spawnPos, Quaternion.identity);
                Debug.Log("[PowerUpSpawner] Spawned PowerUp at " + spawnPos);
                foundSpot = true;
                break;
            }
        }

        if (!foundSpot)
            Debug.LogWarning("[PowerUpSpawner] No free spawn spot found!");
    }

    bool IsOccupied(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 1.2f);
        foreach (var col in colliders)
        {
            if (col.CompareTag("PowerUp"))
                return true;
        }
        return false;
    }
}
