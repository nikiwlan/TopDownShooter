using UnityEngine;
using System.Collections;

public class TankEnemy : EnemyBase, ITimeSlowable
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public LayerMask wallLayer;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDuration = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Audio Clips")]
    public AudioClip attackHitSound;   // Sound wenn Spieler getroffen wird
    public AudioClip hitSound;         // Sound wenn Monster Schaden bekommt
    public AudioClip deathSound;       // Sound beim Tod

    private AudioSource audioSource;

    private Transform playerTransform;

    private float _baseSpeed;
    private bool _isSlowed;
    private float _slowEndTime;
    private Renderer _rend;
    private Color _origColor;

    private bool isAttacking = false;
    private float nextAttackTime = 0f;
    private bool isInAttackRange = false;

    private float _nextHitTime;
    [SerializeField] private float _contactCooldown = 0.4f;

    // ---------------------------------------------------
    protected override void Start()
    {
        base.Start();

        pointsOnKill = 25;
        health = 5;

        playerTransform = player ? player.transform : null;
        _baseSpeed = moveSpeed;

        _rend = GetComponentInChildren<Renderer>();
        if (_rend) _origColor = _rend.material.color;

        if (!animator) animator = GetComponentInChildren<Animator>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // kein 3D Sound
    }

    // ---------------------------------------------------
    public void ApplyTimeSlow(float duration, float factor)
    {
        _isSlowed = true;
        _slowEndTime = Mathf.Max(_slowEndTime, Time.time + duration);
        moveSpeed = _baseSpeed * factor;
        if (_rend) _rend.material.color = Color.cyan;
    }

    // ---------------------------------------------------
    void Update()
    {
        if (_isSlowed && Time.time >= _slowEndTime)
        {
            _isSlowed = false;
            moveSpeed = _baseSpeed;
            if (_rend) _rend.material.color = _origColor;
        }

        if (playerTransform == null || animator == null) return;
        if (animator.GetBool("isDead")) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        isInAttackRange = distance <= attackRange;

        if (!isAttacking && !isInAttackRange)
        {
            MoveTowardsPlayer();
            animator.SetFloat("Speed", moveSpeed);
        }
        else animator.SetFloat("Speed", 0f);

        if (!isAttacking && isInAttackRange && Time.time >= nextAttackTime)
            StartCoroutine(AttackRoutine());
    }

    private void MoveTowardsPlayer()
    {
        Vector3 dir = (playerTransform.position - transform.position).normalized;
        dir.y = 0;

        if (!Physics.Raycast(transform.position, dir, out RaycastHit hit, moveSpeed * Time.deltaTime + 0.2f, wallLayer))
            transform.position += dir * moveSpeed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), 0.2f);
        }
    }

    // ---------------------------------------------------
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetBool("isAttacking", true);

        yield return new WaitForSeconds(attackDuration * 0.5f);

        // Spieler trifft → Attack-Hit-Sound hier!
        if (attackHitSound)
            audioSource.PlayOneShot(attackHitSound);

        if (player != null)
            player.TakeDamage(1);

        yield return new WaitForSeconds(attackDuration * 0.5f);

        isAttacking = false;
        animator.SetBool("isAttacking", false);

        nextAttackTime = Time.time + attackCooldown;
    }

    // ---------------------------------------------------
    public override void TakeDamage(int amount)
    {
        // Monster bekommt Schaden → HitSound
        if (hitSound)
            audioSource.PlayOneShot(hitSound);

        base.TakeDamage(amount);
    }

    // ---------------------------------------------------
    protected override void Die()
    {
        if (deathSound)
            audioSource.PlayOneShot(deathSound);

        if (animator)
        {
            animator.SetBool("isDead", true);
            moveSpeed = 0f;
        }

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (_rend) _rend.material.color = Color.gray;

        base.Die();
    }

    protected override void OnDeathDestroyed()
    {
        Destroy(gameObject, 2.5f);
    }

    // ---------------------------------------------------
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player") && player != null)
        {
            if (Time.time >= _nextHitTime)
            {
                _nextHitTime = Time.time + _contactCooldown;

                // Auch hier könnte ein AttackHitSound hin, aber du hast gesagt NEIN
                player.TakeDamage(1);
            }
        }
    }
}
