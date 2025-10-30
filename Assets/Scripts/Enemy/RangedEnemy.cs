using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RangedEnemy : EnemyBase
{
    public float moveSpeed = 3f;
    public float attackRange = 8f;
    public float shootCooldown = 1.5f;
    public GameObject projectilePrefab;

    private Rigidbody rb;
    private Transform playerTransform;
    private float shootTimer;

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
        playerTransform = player?.transform;
    }

    void Update()
    {
        shootTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        Vector3 dir = (playerTransform.position - transform.position);
        dir.y = 0;
        dir.Normalize();

        if (distance > attackRange)
        {
            rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
        }
        else if (shootTimer <= 0f)
        {
            Shoot(dir);
            shootTimer = shootCooldown;
        }
    }

    void Shoot(Vector3 dir)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position + dir * 1.5f, Quaternion.identity);
        Rigidbody rbProj = proj.GetComponent<Rigidbody>();
        rbProj.velocity = dir * 10f;
        Destroy(proj, 5f);
    }
}
