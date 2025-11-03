// RangedEnemy.cs
using UnityEngine;

public class RangedEnemy : EnemyBase, ITimeSlowable
{
    [Header("Movement & Attack Settings")]
    public float moveSpeed = 3f;
    public float attackRange = 8f;
    public float approachDistance = 6f;
    public float stopThreshold = 0.5f;
    public float shootCooldown = 1.5f;
    public GameObject projectilePrefab;
    public LayerMask wallLayer;

    private Transform playerTransform;
    private float shootTimer;
    private bool isWithinRange;

    // TimeSlow-Interna
    private float _baseSpeed;
    private bool _isSlowed;
    private float _slowEndTime;
    private Renderer _rend;
    private Color _origColor;

    protected override void Start()
    {
        base.Start();
        pointsOnKill = 15;
        playerTransform = player?.transform;

        _baseSpeed = moveSpeed;
        _rend = GetComponentInChildren<Renderer>();
        if (_rend) _origColor = _rend.material.color;
    }

    public void ApplyTimeSlow(float duration, float factor)
    {
        _isSlowed = true;
        _slowEndTime = Mathf.Max(_slowEndTime, Time.time + duration);
        moveSpeed = _baseSpeed * factor;
        if (_rend) _rend.material.color = Color.cyan;
        Debug.Log($"[RangedEnemy] TimeSlow aktiv für {duration:0.##}s @ x{factor}");
    }

    void Update()
    {
        if (_isSlowed && Time.time >= _slowEndTime)
        {
            _isSlowed = false;
            moveSpeed = _baseSpeed;
            if (_rend) _rend.material.color = _origColor;
            Debug.Log("[RangedEnemy] TimeSlow Ende → Speed reset");
        }

        if (playerTransform == null) return;

        shootTimer -= Time.deltaTime;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0;
        float distance = toPlayer.magnitude;
        Vector3 direction = toPlayer.normalized;

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

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
        }

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
        var projectile = projGO.GetComponent<ProjectileEnemy>();
        if (projectile != null) projectile.Init(dir);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // optional: call base for logging
        base.OnTriggerEnter(other);

        Debug.Log($"[RangedEnemy] TRIGGER with {other.name} (tag={other.tag})");

        if (other.CompareTag("Player") && player != null)
        {
            Die();
        }
    }
}
