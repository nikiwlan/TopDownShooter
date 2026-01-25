using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Power-Up Einstellungen")]
    public GameObject[] powerUps;
    public float spawnInterval = 5f;

    [Tooltip("Wie weit maximal vom Spieler entfernt spawnen?")]
    public float spawnRadius = 6f;

    [Tooltip("Wie nah darf ein PowerUp MINDESTENS am Spieler spawnen?")]
    public float spawnMinDistance = 2f;

    [Tooltip("In welcher Höhe sollen PowerUps gespawnt werden?")]
    public float spawnHeight = 5f;

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
        int maxAttempts = 20;

        Debug.Log("\n========== POWERUP SPAWN START ==========");

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Zufälliger Punkt um Spieler herum
            Vector2 offset2D = Random.insideUnitCircle * spawnRadius;

            Vector3 spawnPos = new Vector3(
                player.position.x + offset2D.x,
                spawnHeight,
                player.position.z + offset2D.y
            );

            Debug.Log($"\n[Attempt {attempt + 1}/{maxAttempts}]");
            Debug.Log($" → Test SpawnPos: X={spawnPos.x:F2}, Y={spawnPos.y:F2}, Z={spawnPos.z:F2}");

            // 1) Mindestabstand zum Spieler
            float distToPlayer = Vector3.Distance(player.position, spawnPos);
            Debug.Log($"   • Distanz zum Spieler: {distToPlayer:F2} (min {spawnMinDistance})");

            if (distToPlayer < spawnMinDistance)
            {
                Debug.Log($"   ✖ Abgelehnt: Zu nah am Spieler! → Position: {spawnPos}");
                continue;
            }

            // 2) Innerhalb Arena?
            // HIER GEÄNDERT: Nutzung der statischen Methode aus ArenaSettings (falls du sie dort eingebaut hast)
            // Oder wir prüfen es direkt hier mit den statischen Werten:
            if (!IsInsideArena(spawnPos))
            {
                // FEHLER KORRIGIERT: Hier stand vorher minX/maxX, das gibt es nicht mehr.
                // Jetzt nutzen wir ArenaSettings.MinX etc. für den Log.
                Debug.Log($"   ✖ Abgelehnt: Außerhalb Arena! → SpawnPos: {spawnPos}, " +
                          $"ArenaBounds X({ArenaSettings.MinX}–{ArenaSettings.MaxX}), Z({ArenaSettings.MinZ}–{ArenaSettings.MaxZ})");
                continue;
            }

            // 3) Blockiert?
            if (IsOccupied(spawnPos))
            {
                Debug.Log($"   ✖ Abgelehnt: Blockiert! Kollisionspunkt: {spawnPos}");
                continue;
            }

            // → Gültiger Spawnpunkt
            GameObject prefab = powerUps[Random.Range(0, powerUps.Length)];
            Instantiate(prefab, spawnPos, Quaternion.identity);

            Debug.Log($"   ✔ SUCCESS → PowerUp gespawnt bei {spawnPos}");
            Debug.Log("========== SPAWN FINISHED ==========\n");
            return;
        }

        Debug.LogWarning("❌ Kein gültiger Spawnpunkt nach allen Versuchen!");
        Debug.Log("========== SPAWN FAILED ==========\n");
    }

    bool IsInsideArena(Vector3 pos)
    {
        // Zugriff erfolgt direkt über den Klassennamen
        bool inside = pos.x > ArenaSettings.MinX && pos.x < ArenaSettings.MaxX &&
                      pos.z > ArenaSettings.MinZ && pos.z < ArenaSettings.MaxZ;
        return inside;
    }

    bool IsOccupied(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 1.2f);

        if (colliders.Length == 0)
        {
            Debug.Log("   • Collision Check: Keine Kollisionen.");
            return false;
        }

        Debug.Log($"   • Collision Check: {colliders.Length} Treffer");

        foreach (var col in colliders)
        {
            Debug.Log($"     → Hit Collider: '{col.name}' | Tag: '{col.tag}' | Layer: {LayerMask.LayerToName(col.gameObject.layer)}");

            if (col.CompareTag("PowerUp") || col.CompareTag("Wall"))
            {
                Debug.Log($"     ✖ BLOCKED durch Objekt: '{col.name}' (Tag: {col.tag}) an Position {position}");
                return true;
            }
        }

        Debug.Log("   ✔ Keine blockierende Kollision (nur Boden oder ignorierbare Layer)");
        return false;
    }
}
