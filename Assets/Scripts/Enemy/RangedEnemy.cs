using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("Movement & Attack Settings")]
    public float moveSpeed = 3f;
    public float attackRange = 8f;       // maximale Schussreichweite
    public float approachDistance = 6f;  // bis hierher läuft der Gegner
    public float stopThreshold = 0.5f;
    public float shootCooldown = 1.5f;
    public GameObject projectilePrefab;
    public LayerMask wallLayer;

    private Transform playerTransform;
    private float shootTimer;
    private bool isWithinRange;

    protected override void Start()
    {
        base.Start();
        pointsOnKill = 15; // RangeEnemy = 15 Punkte
        playerTransform = player?.transform;
    }



    void Update()
    {
        if (playerTransform == null) return;

        shootTimer -= Time.deltaTime;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0;
        float distance = toPlayer.magnitude;
        Vector3 direction = toPlayer.normalized;

        // Bewegung nur, wenn Spieler weiter entfernt ist
        if (distance > approachDistance + stopThreshold)
        {
            if (!Physics.Raycast(transform.position, direction, out RaycastHit hit, moveSpeed * Time.deltaTime + 0.2f, wallLayer))
            {
                transform.position += direction * moveSpeed * Time.deltaTime;
            }
            isWithinRange = false;
        }
        else
        {
            isWithinRange = true;
        }

        // Gegner schaut immer zum Spieler
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
        }

        // Schießen, wenn in Reichweite
        if (isWithinRange && distance <= attackRange && shootTimer <= 0f)
        {
            Shoot(direction);
            shootTimer = shootCooldown;
        }
    }

    void Shoot(Vector3 dir)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = transform.position + dir * 1.2f + Vector3.up * 0.5f;
        GameObject projGO = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));

        ProjectileEnemy projectile = projGO.GetComponent<ProjectileEnemy>();
        if (projectile != null)
            projectile.Init(dir);
    }
}
