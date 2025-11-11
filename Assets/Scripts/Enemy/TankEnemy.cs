using UnityEngine;
using System.Collections;

public class TankEnemy : EnemyBase, ITimeSlowable
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public LayerMask wallLayer;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDuration = 1.5f;  // Dauer der Attack-Animation
    [SerializeField] private float attackCooldown = 1.5f;  // Pause zwischen Attacken

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Transform playerTransform;

    // TimeSlow-Interna
    private float _baseSpeed;
    private bool _isSlowed;
    private float _slowEndTime;
    private Renderer _rend;
    private Color _origColor;

    // Angriff / Bewegung
    private bool isAttacking = false;
    private float nextAttackTime = 0f;
    private bool isInAttackRange = false;

    // Kontakt-Treffer-Schutz
    private float _nextHitTime;
    [SerializeField] private float _contactCooldown = 0.4f;

    // ------------------------------------------
    // INIT
    // ------------------------------------------
    protected override void Start()
    {
        base.Start(); // base.Start() setzt player in EnemyBase

        pointsOnKill = 25;
        health = 5;

        playerTransform = player ? player.transform : null;
        _baseSpeed = moveSpeed;

        _rend = GetComponentInChildren<Renderer>();
        if (_rend) _origColor = _rend.material.color;

        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    // ------------------------------------------
    // TIME-SLOW
    // ------------------------------------------
    public void ApplyTimeSlow(float duration, float factor)
    {
        _isSlowed = true;
        _slowEndTime = Mathf.Max(_slowEndTime, Time.time + duration);
        moveSpeed = _baseSpeed * factor;
        if (_rend) _rend.material.color = Color.cyan;
        Debug.Log($"[TankEnemy] TimeSlow aktiv für {duration:0.##}s @ x{factor}");
    }

    // ------------------------------------------
    // UPDATE LOOP
    // ------------------------------------------
    void Update()
    {
        if (_isSlowed && Time.time >= _slowEndTime)
        {
            _isSlowed = false;
            moveSpeed = _baseSpeed;
            if (_rend) _rend.material.color = _origColor;
        }

        if (playerTransform == null || animator == null) return;

        // Wenn tot → nichts mehr tun
        if (animator.GetBool("isDead")) return;

        // Abstand zum Spieler
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        isInAttackRange = distance <= attackRange;

        // Wenn nicht angreifend und außerhalb der Range → Bewegen
        if (!isAttacking && !isInAttackRange)
        {
            MoveTowardsPlayer();
            animator.SetFloat("Speed", moveSpeed);
        }
        else
        {
            // Innerhalb der Range oder beim Angriff → stehen bleiben
            animator.SetFloat("Speed", 0f);
        }

        // Wenn in Reichweite und Angriff möglich → Attack starten
        if (!isAttacking && isInAttackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    // ------------------------------------------
    // BEWEGUNG ZUM SPIELER
    // ------------------------------------------
    private void MoveTowardsPlayer()
    {
        Vector3 dir = (playerTransform.position - transform.position).normalized;
        dir.y = 0;

        // Wände vermeiden
        if (!Physics.Raycast(transform.position, dir, out RaycastHit hit, moveSpeed * Time.deltaTime + 0.2f, wallLayer))
        {
            transform.position += dir * moveSpeed * Time.deltaTime;
        }

        // Sanft rotieren
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
        }
    }

    // ------------------------------------------
    // ATTACK
    // ------------------------------------------
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetBool("isAttacking", true);

        // Schlag nach halber Dauer
        yield return new WaitForSeconds(attackDuration * 0.5f);
        if (player != null)
            player.TakeDamage(1);

        // Rest der Animation abwarten
        yield return new WaitForSeconds(attackDuration * 0.5f);

        // Angriff beenden
        isAttacking = false;
        animator.SetBool("isAttacking", false);

        // Cooldown aktivieren – währenddessen bleibt er stehen
        nextAttackTime = Time.time + attackCooldown;
    }

    // ------------------------------------------
    // SCHADEN & TOD
    // ------------------------------------------
    public override void TakeDamage(int amount)
    {
        if (animator != null && animator.GetBool("isDead")) return;

        health -= amount;
        if (debug)
            Debug.Log($"[TankEnemy] Nimmt {amount} Schaden → verbleibend: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    protected override void Die()
    {
        if (animator)
        {
            animator.SetBool("isDead", true);
            moveSpeed = 0f;
        }

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (_rend) _rend.material.color = Color.gray;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(pointsOnKill);

        Destroy(gameObject, 2.5f);
    }

    // ------------------------------------------
    // TRIGGER-KONTAKT
    // ------------------------------------------
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player") && player != null)
        {
            if (Time.time >= _nextHitTime)
            {
                _nextHitTime = Time.time + _contactCooldown;
                player.TakeDamage(1);
            }
        }
    }
}
