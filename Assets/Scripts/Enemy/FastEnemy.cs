using UnityEngine;

public class FastEnemy : EnemyBase, ITimeSlowable
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float attackRange = 1.8f;

    [Header("Attack Settings")]
    public float attackDamageDelay = 0.8f;
    public AudioClip hitSound;
    [Range(0f, 1f)] public float hitVolume = 1f;

    [Header("Death Sound")]
    public AudioClip deathSound;           // 🔊 Sound wenn er durch Bullet stirbt
    private AudioSource deathAudio;        // 🎧 Eigener AudioSource dafür

    private Transform playerTransform;
    private Animator animator;

    private bool isAttacking = false;
    private bool hasDealtDamage = false;
    private float attackTimer = 0f;

    private AudioSource attackAudio;
    private bool isDead = false;

    private float baseSpeed;
    private bool isSlowed;
    private float slowEndTime;

    private readonly string RUN_STATE = "Injured Run";
    private readonly string ATTACK_STATE = "Zombie Attack";
    private readonly string DIE_TRIGGER = "Die";

    protected override void Start()
    {
        base.Start();

        playerTransform = player?.transform;
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.Play(RUN_STATE);

        attackAudio = gameObject.AddComponent<AudioSource>();
        attackAudio.playOnAwake = false;
        attackAudio.loop = false;
        attackAudio.spatialBlend = 0f;
        attackAudio.volume = hitVolume;

        // 🔊 Death AudioSource
        deathAudio = gameObject.AddComponent<AudioSource>();
        deathAudio.playOnAwake = false;
        deathAudio.loop = false;
        deathAudio.spatialBlend = 0f;

        baseSpeed = moveSpeed;

        pointsOnKill = 10;
    }


    void Update()
    {
        if (isDead || playerTransform == null)
            return;

        HandleTimeSlow();

        if (isAttacking)
        {
            HandleAttack();
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= attackRange)
        {
            StartAttack();
            return;
        }

        MoveTowardsPlayer();
    }

    private void MoveTowardsPlayer()
    {
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0;
        Vector3 n = dir.normalized;

        transform.position += n * (moveSpeed * Time.deltaTime);

        if (n.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(n);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 0.2f);
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        hasDealtDamage = false;
        attackTimer = 0f;

        moveSpeed = 0f;

        animator.ResetTrigger(DIE_TRIGGER);
        animator.Play(ATTACK_STATE, 0, 0f);
    }

    private void HandleAttack()
    {
        attackTimer += Time.deltaTime;

        if (!hasDealtDamage && attackTimer >= attackDamageDelay)
        {
            hasDealtDamage = true;

            if (hitSound)
                attackAudio.PlayOneShot(hitSound);

            player?.TakeDamage(1);
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);

        if (info.IsName(ATTACK_STATE) && info.normalizedTime >= 0.6f)
        {
            TriggerDeath();
        }
    }

    private void TriggerDeath()
    {
        if (isDead) return;

        isDead = true;
        isAttacking = false;

        animator.SetTrigger(DIE_TRIGGER);

        base.Die();

        Destroy(gameObject, 2f);
    }

    protected override void Die()
    {
        // ⚠️ Nur hier wird er von Spieler/Bullet getötet
        if (!isDead && deathSound != null)
        {
            deathAudio.PlayOneShot(deathSound);
        }

        TriggerDeath();
    }


    // Time Slow
    public void ApplyTimeSlow(float duration, float factor)
    {
        isSlowed = true;
        slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
        moveSpeed = baseSpeed * factor;
    }

    private void HandleTimeSlow()
    {
        if (isSlowed && Time.time >= slowEndTime)
        {
            isSlowed = false;
            moveSpeed = baseSpeed;
        }
    }
}
