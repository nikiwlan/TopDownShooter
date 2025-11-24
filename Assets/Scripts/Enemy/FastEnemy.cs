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

    [Header("Death Sounds")]
    public AudioClip normalDeathHitSound;   // NEU → abgespielt direkt vor normalem DeathSound
    public AudioClip normalDeathSound;      // normaler Tod (gibt Score)
    public AudioClip attackDeathSound;      // Tod nach eigenem Angriff (kein Score)

    private AudioSource audioSrc;

    [Header("Gate Hit Sound")]
    public AudioClip gateHitSound;
    public float gateHitVolume = 1f;

    private Transform playerTransform;
    private Animator animator;

    private bool isAttacking = false;
    private bool hasDealtDamage = false;
    private float attackTimer = 0f;

    private bool isDead = false;
    private bool deathByAttack = false;

    private float baseSpeed;
    private bool isSlowed;
    private float slowEndTime;

    private readonly string RUN_STATE = "Injured Run";
    private readonly string ATTACK_STATE = "Zombie Attack";
    private readonly string DIE_TRIGGER = "Die";


    // ---------------------- START ----------------------
    protected override void Start()
    {
        base.Start();

        playerTransform = player?.transform;
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.Play(RUN_STATE);

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.loop = false;
        audioSrc.spatialBlend = 0f;

        baseSpeed = moveSpeed;
        pointsOnKill = 10;
    }


    // ---------------------- UPDATE ----------------------
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


    // ---------------------- MOVEMENT ----------------------
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


    // ---------------------- ATTACK ----------------------
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

            if (hitSound != null)
                audioSrc.PlayOneShot(hitSound, hitVolume);

            player?.TakeDamage(1);

            deathByAttack = true;
        }

        var info = animator.GetCurrentAnimatorStateInfo(0);

        if (info.IsName(ATTACK_STATE) && info.normalizedTime >= 0.6f)
        {
            TriggerDeath();
        }
    }


    // ---------------------- DEATH HANDLING ----------------------
    private void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;

        animator.SetTrigger(DIE_TRIGGER);

        // Score nur, wenn KEIN Angriffstod
        if (!deathByAttack)
            base.Die();
        else
            DisableAllColliders(); // kein Score bei AttackDeath

        // ---------- SOUND LOGIK ----------
        if (deathByAttack)
        {
            // Angriffstod → nur attackDeathSound
            if (attackDeathSound != null)
                audioSrc.PlayOneShot(attackDeathSound);
        }
        else
        {
            // normaler Tod → ZWEI SOUNDS nacheinander
            if (normalDeathHitSound != null)
                audioSrc.PlayOneShot(normalDeathHitSound);

            if (normalDeathSound != null)
                audioSrc.PlayOneShot(normalDeathSound);
        }

        Destroy(gameObject, 2f);
    }


    protected override void Die()
    {
        if (!isDead)
            TriggerDeath();
    }


    // ---------------------- GATE HIT ----------------------
    protected override void OnGateHit()
    {
        if (gateHitSound != null)
            audioSrc.PlayOneShot(gateHitSound, gateHitVolume);
    }


    // ---------------------- TRIGGER ----------------------
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Gate"))
        {
            OnGateHit();
        }

        if (other.CompareTag("Player") && player != null)
        {
            player.TakeDamage(1);
        }
    }


    // ---------------------- TIME SLOW ----------------------
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
