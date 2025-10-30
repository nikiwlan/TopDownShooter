using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RangedEnemy : EnemyBase
{
    [Header("Movement & Attack Settings")]
    public float moveSpeed = 3f;
    public float attackRange = 8f;       // maximale Schussreichweite
    public float approachDistance = 6f;  // Distanz, bis wohin er läuft
    public float stopThreshold = 0.5f;   // wie nah er an approachDistance heranlaufen darf
    public float shootCooldown = 1.5f;
    public GameObject projectilePrefab;

    private Rigidbody rb;
    private Transform playerTransform;
    private float shootTimer;
    private bool isWithinRange;

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
        playerTransform = player?.transform;
    }

    void Update()
    {
        shootTimer -= Time.deltaTime;

        // Wenn Spieler in Reichweite → schießen
        if (isWithinRange && shootTimer <= 0f)
        {
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            dir.y = 0;
            Shoot(dir);
            shootTimer = shootCooldown;
        }
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0;
        float distance = toPlayer.magnitude;
        Vector3 direction = toPlayer.normalized;

        // 🧠 Lauf nur, wenn Spieler weiter als die gewünschte Distanz ist
        if (distance > approachDistance + stopThreshold)
        {
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
            isWithinRange = false;
        }
        else if (distance < approachDistance - stopThreshold)
        {
            // Spieler zu nah → einfach stehen bleiben (nicht rückwärtslaufen)
            isWithinRange = true;
        }
        else
        {
            // Wir sind in der „Komfortzone“ → nicht bewegen
            isWithinRange = true;
        }

        // Optional: Gegner schaut zum Spieler
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 0.2f));
        }
    }

    void Shoot(Vector3 dir)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = transform.position + dir * 1.2f + Vector3.up * 0.5f;
        GameObject projGO = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));

        // Richtung direkt übergeben
        ProjectileEnemy projectile = projGO.GetComponent<ProjectileEnemy>();
        if (projectile != null)
            projectile.Init(dir);
    }
}
