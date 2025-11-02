using UnityEngine;

public class TankEnemy : EnemyBase
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public LayerMask wallLayer;

    private Transform playerTransform;

    protected override void Start()
    {
        base.Start();
        pointsOnKill = 25; // TankEnemy = 25 Punkte
        playerTransform = player?.transform;
        health = 5; // Tank ist robuster
    }


    void Update()
    {
        if (playerTransform == null) return;

        Vector3 dir = (playerTransform.position - transform.position).normalized;
        dir.y = 0;

        // Blockierung durch Wand prüfen
        if (!Physics.Raycast(transform.position, dir, out RaycastHit hit, moveSpeed * Time.deltaTime + 0.2f, wallLayer))
        {
            transform.position += dir * moveSpeed * Time.deltaTime;
        }

        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player.TakeDamage(1);
        }
    }
}
