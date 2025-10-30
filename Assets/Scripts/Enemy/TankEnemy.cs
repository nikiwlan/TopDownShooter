using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankEnemy : EnemyBase
{
    public float moveSpeed = 2f;

    private Rigidbody rb;
    private Transform playerTransform;

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
        playerTransform = player?.transform;
        health = 5;
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;
        Vector3 dir = (playerTransform.position - transform.position).normalized;
        dir.y = 0;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            player.TakeDamage(1);
        }
    }
}
