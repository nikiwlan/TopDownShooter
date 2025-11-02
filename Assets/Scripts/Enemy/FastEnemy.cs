using UnityEngine;

public class FastEnemy : EnemyBase
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public LayerMask wallLayer;

    private Transform playerTransform;

    protected override void Start()
    {
        base.Start();
        pointsOnKill = 10; // FastEnemy = 10 Punkte
        playerTransform = player ? player.transform : null;
    }


    void Update()
    {
        if (!playerTransform) return;

        Vector3 dir = (playerTransform.position - transform.position);
        dir.y = 0f;
        var n = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;

        // DEBUG draw movement intent
        Debug.DrawRay(transform.position + Vector3.up * 0.25f, n * 1.0f, Color.cyan);

        float step = moveSpeed * Time.deltaTime;
        if (!Physics.Raycast(transform.position + Vector3.up * 0.25f, n, out RaycastHit hit, step + 0.2f, wallLayer))
            transform.position += n * step;
        else
            Debug.DrawRay(transform.position + Vector3.up * 0.25f, n * hit.distance, Color.red); // blocked

        if (n != Vector3.zero)
        {
            var targetRot = Quaternion.LookRotation(n);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
        }
    }

    // IMPORTANT: override (not hiding)
    protected override void OnTriggerEnter(Collider other)
    {
        // optional: call base for logging
        base.OnTriggerEnter(other);

        Debug.Log($"[FastEnemy] TRIGGER with {other.name} (tag={other.tag})");

        if (other.CompareTag("Player") && player != null)
        {
            Debug.Log("[FastEnemy] Hit PLAYER -> damage + die");
            player.TakeDamage(1);
            Die();
        }
    }
}
