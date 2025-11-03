// TankEnemy.cs
using UnityEngine;

public class TankEnemy : EnemyBase, ITimeSlowable
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public LayerMask wallLayer;

    private Transform playerTransform;

    // TimeSlow-Interna
    private float _baseSpeed;
    private bool _isSlowed;
    private float _slowEndTime;
    private Renderer _rend;
    private Color _origColor;

    private float _nextHitTime;        // Debounce-Zeitpunkt
    [SerializeField] private float _contactCooldown = 0.4f; // 400ms Sperre

    protected override void Start()
    {
        base.Start();
        pointsOnKill = 25;
        playerTransform = player?.transform;
        health = 5;

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
        Debug.Log($"[TankEnemy] TimeSlow aktiv für {duration:0.##}s @ x{factor}");
    }

    void Update()
    {
        if (_isSlowed && Time.time >= _slowEndTime)
        {
            _isSlowed = false;
            moveSpeed = _baseSpeed;
            if (_rend) _rend.material.color = _origColor;
            Debug.Log("[TankEnemy] TimeSlow Ende → Speed reset");
        }

        if (playerTransform == null) return;

        Vector3 dir = (playerTransform.position - transform.position).normalized;
        dir.y = 0;

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

    protected override void OnTriggerEnter(Collider other)
    {
        // optional: call base for logging
        base.OnTriggerEnter(other);

        Debug.Log($"[FastEnemy] TRIGGER with {other.name} (tag={other.tag})");

        if (other.CompareTag("Player") && player != null)
        {
            player.TakeDamage(1);
            Die();
        }
    }
}
