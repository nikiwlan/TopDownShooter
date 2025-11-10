// FastEnemy.cs
using UnityEngine;

public class FastEnemy : EnemyBase, ITimeSlowable
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public LayerMask wallLayer;

    private Transform playerTransform;
    private Animator animator;

    // TimeSlow-Interna
    private float _baseSpeed;
    private bool _isSlowed;
    private bool _didHitPlayer;
    private float _slowEndTime;
    private Renderer _rend;
    private Color _origColor;

    protected override void Start()
    {
        base.Start();
        pointsOnKill = 10;
        playerTransform = player ? player.transform : null;

        animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.Play("Injured Run");  // Name deines Mixamo-Run-Clips


        _baseSpeed = moveSpeed;
        _rend = GetComponentInChildren<Renderer>();
        if (_rend) _origColor = _rend.material.color;
    }

    public void ApplyTimeSlow(float duration, float factor)
    {
        // Start/Verlängerung des Effekts
        _isSlowed = true;
        _slowEndTime = Mathf.Max(_slowEndTime, Time.time + duration);
        moveSpeed = _baseSpeed * factor;
        if (_rend) _rend.material.color = Color.cyan;
        Debug.Log($"[FastEnemy] TimeSlow aktiv für {duration:0.##}s @ x{factor}");
    }

    void Update()
    {
        // TimeSlow Ablauf prüfen
        if (_isSlowed && Time.time >= _slowEndTime)
        {
            _isSlowed = false;
            moveSpeed = _baseSpeed;
            if (_rend) _rend.material.color = _origColor;
            Debug.Log("[FastEnemy] TimeSlow Ende → Speed reset");
        }

        if (!playerTransform) return;

        Vector3 dir = (playerTransform.position - transform.position);
        dir.y = 0f;
        var n = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;

        Debug.DrawRay(transform.position + Vector3.up * 0.25f, n * 1.0f, Color.cyan);

        float step = moveSpeed * Time.deltaTime;
        if (!Physics.Raycast(transform.position + Vector3.up * 0.25f, n, out RaycastHit hit, step + 0.2f, wallLayer))
            transform.position += n * step;

        if (n != Vector3.zero)
        {
            var targetRot = Quaternion.LookRotation(n);
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
            Die();
        }
    }
    protected override void Die()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // Optional: Bewegungen stoppen
        moveSpeed = 0f;

        // Danach zerstören, nachdem Animation fertig ist (z. B. nach 2 Sekunden)
        Destroy(gameObject, 2f);
    }
}
