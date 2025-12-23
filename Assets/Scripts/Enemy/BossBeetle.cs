using UnityEngine;
using System.Collections;

public class BossBeetle : EnemyBase
{
    // ==========================
    // BODY HIT (Phase 0)
    // ==========================
    [Header("Body Hit (Phase 0)")]
    public AudioClip bodyHitSound;
    [Range(0f, 1f)] public float bodyHitVolume = 1f;

    // ==========================
    // HEALTH / PHASES
    // ==========================
    [Header("Health / Phases")]
    public int maxHealth = 30;

    // ==========================
    // MOVEMENT
    // ==========================
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 12f;
    [SerializeField] private float runStartRange = 8f;
    public LayerMask wallLayer;

    // ==========================
    // RUN (PHASE 1) - PARTIAL STEERING + WALL STUN + COOLDOWN
    // ==========================
    [Header("Run (Phase 1)")]
    [SerializeField] private float runMaxTime = 1.2f;             // how long a charge lasts
    [SerializeField] private float runStopDistance = 1.2f;        // stop charge when close enough
    [SerializeField] private float runSteerStrength = 0.7f;       // base steering strength
    [SerializeField] private float runSteerNearMultiplier = 0.2f; // near player: less correction
    [SerializeField] private float runSteerFarMultiplier = 1.0f;  // far: more correction
    [SerializeField] private float runSteerDistanceRange = 8f;    // dist range for far->near blend
    [SerializeField] private float runWallCheckDistance = 0.9f;   // how far ahead to check for wall

    [Header("Run Cooldown (Phase 1)")]
    [SerializeField] private float runCooldownAfterRun = 2.0f;    // after a run ends: pause before next run
    [SerializeField] private float runCooldownAfterStun = 2.0f;   // after stun ends: pause before next run
    private float nextRunAllowedTime = 0f;

    [Header("Wall Stun (Phase 1)")]
    [SerializeField] private float wallStunDuration = 3f;
    [Tooltip("Optional: prefab (e.g. icon) spawned while stunned. It will be parented to this enemy.")]
    [SerializeField] private GameObject stunIconPrefab;
    [Tooltip("Optional: where the stun icon should attach. If null, it uses this transform.")]
    [SerializeField] private Transform stunIconAnchor;

    private bool isRunning;
    private float runEndTime;
    private Vector3 runDir;

    private bool isStunned;
    private float stunEndTime;
    private GameObject stunIconInstance;

    // ==========================
    // RAGE (SUPER SIMPLE TIMER)
    // ==========================
    [Header("Rage")]
    [SerializeField] private float rageDuration = 2f;
    [SerializeField] private bool immuneDuringRage = true;

    private float rageEndTime = -1f;
    private bool IsRaging => Time.time < rageEndTime;

    private int lastPhase = -1;

    // ==========================
    // ATTACK ORIGINS
    // ==========================
    [Header("Attack Origins (per phase)")]
    [SerializeField] private Transform attackOrigin1;
    [SerializeField] private Transform attackOrigin2;
    [SerializeField] private Transform attackOrigin3;

    // ==========================
    // ATTACK SETTINGS
    // ==========================
    [Header("Attack Settings")]
    [SerializeField] private float attackRange1 = 2f;
    [SerializeField] private float attackRange2 = 2f;
    [SerializeField] private float attackRange3 = 2f;

    [SerializeField] private int attackDamage1 = 1;
    [SerializeField] private int attackDamage2 = 2;
    [SerializeField] private int attackDamage3 = 3;

    [SerializeField] private float attackDuration1 = 1.2f;
    [SerializeField] private float attackDuration2 = 1.2f;
    [SerializeField] private float attackDuration3 = 1.2f;

    [SerializeField] private float attackCooldown1 = 1.5f;
    [SerializeField] private float attackCooldown2 = 1.3f;
    [SerializeField] private float attackCooldown3 = 1.1f;

    // ==========================
    // ANIMATION / FX / AUDIO
    // ==========================
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("VFX")]
    public GameObject tankHitVFX;
    public GameObject tankDeathVFX;

    [Header("Audio Clips")]
    public AudioClip attackHitSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    // ==========================
    // INTERNAL
    // ==========================
    private Transform playerTransform;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;

    private float _nextHitTime;
    [SerializeField] private float _contactCooldown = 0.4f;

    // Animator parameter names
    private const string PARAM_PHASE = "Phase";
    private const string PARAM_CLOSE = "CloseToPlayer";
    private const string PARAM_ISRUN = "IsRunning";
    private const string PARAM_ISDEAD = "Die";
    private const string TRIG_ATTACK = "IsAttacking";

    // Rage bool in Animator
    private const string PARAM_RAGE_BOOL = "Rage";

    // ==========================
    // UNITY
    // ==========================
    protected override void Start()
    {
        base.Start();

        pointsOnKill = 500;
        health = maxHealth;

        playerTransform = player ? player.transform : null;
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (!attackOrigin1) attackOrigin1 = transform;
        if (!attackOrigin2) attackOrigin2 = transform;
        if (!attackOrigin3) attackOrigin3 = transform;

        if (!stunIconAnchor) stunIconAnchor = transform;

        // Init phase + lastPhase (OHNE Rage beim Start)
        lastPhase = GetPhase();
        animator.SetInteger(PARAM_PHASE, lastPhase);

        ApplyAnimatorRunFlag();

        // sicherstellen: Rage Bool aus
        animator.SetBool(PARAM_RAGE_BOOL, false);
    }

    void Update()
    {
        if (!playerTransform || !animator) return;
        if (animator.GetBool(PARAM_ISDEAD)) return;

        UpdatePhaseAndAnimator(); // triggert Rage nur bei echtem Phasenwechsel
        int phase = animator.GetInteger(PARAM_PHASE);

        bool closeForRun = Vector3.Distance(transform.position, playerTransform.position) <= runStartRange;
        animator.SetBool(PARAM_CLOSE, closeForRun);

        ApplyAnimatorRunFlag();

        // --- Stun ---
        if (isStunned)
        {
            if (Time.time >= stunEndTime)
                EndStun();

            animator.SetFloat("Speed", 0f);
            return;
        }


        if (IsRaging)
        {
            animator.SetBool(PARAM_RAGE_BOOL, true);
            animator.SetFloat("Speed", 0f);

            // alles stoppen
            isRunning = false;
            isAttacking = false;

            // falls Rigidbody vorhanden: stop
            Rigidbody rb3D = GetComponent<Rigidbody>();
            if (rb3D) rb3D.velocity = Vector3.zero;

            Rigidbody2D rb2D = GetComponent<Rigidbody2D>();
            if (rb2D) rb2D.velocity = Vector2.zero;

            return;
        }
        else
        {
            animator.SetBool(PARAM_RAGE_BOOL, false);
        }

        // ✅ Während Attack kein Movement
        if (isAttacking)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        // --- Phase 1: Run one-shot + Cooldown ---
        if (phase == 1 && closeForRun && !isRunning && !isAttacking && Time.time >= nextRunAllowedTime)
        {
            StartRun();
            ApplyAnimatorRunFlag();
        }

        // --- During run: move, no attacks ---
        if (isRunning)
        {
            RunMove();
            animator.SetFloat("Speed", isRunning ? runSpeed : 0f);
            ApplyAnimatorRunFlag();
            return;
        }

        // --- Normal movement / attacks ---
        Transform origin = GetOriginForPhase(phase);
        float attackRange = GetAttackRangeForPhase(phase);

        bool isInAttackRange = Vector3.Distance(origin.position, playerTransform.position) <= attackRange;

        float moveSpeed = walkSpeed;

        if (!isInAttackRange)
        {
            MoveTowardsPlayer(moveSpeed, origin);
            animator.SetFloat("Speed", moveSpeed);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }

        if (!isAttacking && isInAttackRange && Time.time >= nextAttackTime)
            StartCoroutine(AttackRoutine(phase));
    }

    private void ApplyAnimatorRunFlag()
    {
        animator.SetBool(PARAM_ISRUN, isRunning);
    }

    // ==========================
    // RUN ABBRECHEN WENN ATTACK STARTET
    // ==========================
    private void AbortRunForAttack()
    {
        if (isRunning)
            StopRunAndCooldown(0f);

        animator.SetFloat("Speed", 0f);
    }

    // ==========================
    // RAGE (TIMER ONLY)
    // ==========================
    private void StartRage()
    {
        rageEndTime = Time.time + rageDuration;

        // sofort alles stoppen
        isAttacking = false;

        if (isRunning)
            StopRunAndCooldown(0f);

        isRunning = false;

        animator.SetBool(PARAM_RAGE_BOOL, true);
        animator.SetFloat("Speed", 0f);

        // Run erst nach Rage erlauben
        nextRunAllowedTime = rageEndTime;
    }

    // ==========================
    // PHASE UPDATE (NUR EINE METHODE!)
    // ==========================
    private void UpdatePhaseAndAnimator()
    {
        int newPhase = GetPhase();
        if (newPhase == lastPhase) return;

        lastPhase = newPhase;
        animator.SetInteger(PARAM_PHASE, newPhase);

        // Rage nur bei echtem Wechsel
        StartRage();
    }

    // ==========================
    // RUN (PHASE 1)
    // ==========================
    private void StartRun()
    {
        isRunning = true;
        runEndTime = Time.time + runMaxTime;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;
        runDir = (toPlayer.sqrMagnitude > 0.0001f) ? toPlayer.normalized : transform.forward;

        ApplyAnimatorRunFlag();
    }

    private void StopRunAndCooldown(float cooldownSeconds)
    {
        isRunning = false;
        ApplyAnimatorRunFlag();

        StartCoroutine(ForceLeaveRunOneFrame());
        nextRunAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
    }

    private IEnumerator ForceLeaveRunOneFrame()
    {
        bool wasClose = animator.GetBool(PARAM_CLOSE);
        animator.SetBool(PARAM_CLOSE, false);
        yield return null;
        animator.SetBool(PARAM_CLOSE, wasClose);
    }

    private void RunMove()
    {
        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (Time.time >= runEndTime || dist <= runStopDistance)
        {
            StopRunAndCooldown(runCooldownAfterRun);
            return;
        }

        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, runDir, runWallCheckDistance, wallLayer))
        {
            StopRunAndCooldown(0f);
            StartStun();
            return;
        }

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;
        Vector3 targetDir = (toPlayer.sqrMagnitude > 0.0001f) ? toPlayer.normalized : runDir;

        float t = Mathf.Clamp01(dist / runSteerDistanceRange);
        float steerMul = Mathf.Lerp(runSteerNearMultiplier, runSteerFarMultiplier, t);
        float steer = runSteerStrength * steerMul;

        runDir = Vector3.Slerp(runDir, targetDir, steer * Time.deltaTime).normalized;

        if (!Physics.Raycast(transform.position, runDir, runSpeed * Time.deltaTime + 0.2f, wallLayer))
            transform.position += runDir * runSpeed * Time.deltaTime;

        if (runDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(runDir),
                0.25f
            );
        }
    }

    private void StartStun()
    {
        isStunned = true;
        stunEndTime = Time.time + wallStunDuration;

        if (stunIconPrefab && !stunIconInstance)
        {
            stunIconInstance = Instantiate(stunIconPrefab, stunIconAnchor.position, Quaternion.identity, stunIconAnchor);
        }
    }

    private void EndStun()
    {
        isStunned = false;

        if (stunIconInstance)
        {
            Destroy(stunIconInstance);
            stunIconInstance = null;
        }

        nextRunAllowedTime = Time.time + Mathf.Max(0f, runCooldownAfterStun);
    }

    // ==========================
    // HIT HANDLING (CALLED BY BULLET)
    // ==========================
    public void OnHeadHit(int damage, Vector3 hitDir, Vector3 hitPoint)
    {
        int phase = GetPhase();

        if (phase == 0)
        {
            TakeDamage(damage, hitDir, hitPoint);
            return;
        }

        if (phase == 1)
        {
            if (isRunning)
                TakeDamage(damage, hitDir, hitPoint);

            return;
        }

        // Phase 2: later
    }

    public void OnBodyHit(Vector3 hitPoint)
    {
        if (bodyHitSound)
            AudioManager.Instance.PlaySound3D(bodyHitSound, hitPoint, bodyHitVolume);
    }

    private int GetPhase()
    {
        return (health > 20) ? 0 : (health > 10) ? 1 : 2;
    }

    // ==========================
    // MOVEMENT / ATTACK LOGIC
    // ==========================
    private Transform GetOriginForPhase(int phase)
    {
        return phase switch
        {
            0 => attackOrigin1,
            1 => attackOrigin2,
            _ => attackOrigin3
        };
    }

    private float GetAttackRangeForPhase(int phase)
    {
        return phase switch
        {
            0 => attackRange1,
            1 => attackRange2,
            _ => attackRange3
        };
    }

    private int GetAttackDamageForPhase(int phase)
    {
        return phase switch
        {
            0 => attackDamage1,
            1 => attackDamage2,
            _ => attackDamage3
        };
    }

    private float GetAttackDurationForPhase(int phase)
    {
        return phase switch
        {
            0 => attackDuration1,
            1 => attackDuration2,
            _ => attackDuration3
        };
    }

    private float GetAttackCooldownForPhase(int phase)
    {
        return phase switch
        {
            0 => attackCooldown1,
            1 => attackCooldown2,
            _ => attackCooldown3
        };
    }

    private void MoveTowardsPlayer(float speed, Transform origin)
    {
        Vector3 dir = (playerTransform.position - origin.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        if (!Physics.Raycast(transform.position, dir, speed * Time.deltaTime + 0.2f, wallLayer))
            transform.position += dir * speed * Time.deltaTime;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            0.2f
        );
    }

    private IEnumerator AttackRoutine(int phase)
    {
        AbortRunForAttack();

        isAttacking = true;
        animator.SetTrigger(TRIG_ATTACK);

        yield return new WaitForSeconds(GetAttackDurationForPhase(phase) * 0.5f);

        if (attackHitSound)
            AudioManager.Instance.PlaySound3D(attackHitSound, transform.position);

        player?.TakeDamage(GetAttackDamageForPhase(phase));

        yield return new WaitForSeconds(GetAttackDurationForPhase(phase) * 0.5f);

        isAttacking = false;
        nextAttackTime = Time.time + GetAttackCooldownForPhase(phase);
    }

    // ==========================
    // DAMAGE / DEATH
    // ==========================
    public override void TakeDamage(int amount, Vector3 hitDir, Vector3 hitPoint = default)
    {
        if (health <= 0) return;

        // ✅ FIX: statt isRaging -> IsRaging
        if (IsRaging && immuneDuringRage)
            return;

        if (hitSound)
            AudioManager.Instance.PlaySound3D(hitSound, transform.position);

        health -= amount;

        if (tankHitVFX)
        {
            GameObject vfx = Instantiate(
                tankHitVFX,
                hitPoint == Vector3.zero ? transform.position + Vector3.up * 1.5f : hitPoint,
                Quaternion.identity
            );
            Destroy(vfx, 0.5f);
        }

        if (health <= 0)
        {
            Die();
            return;
        }

        // ❗ Phase/Rage wird NICHT hier getriggert.
        // Das macht UpdatePhaseAndAnimator() zentral.
    }

    protected override void Die()
    {
        isRunning = false;
        ApplyAnimatorRunFlag();

        if (isStunned) EndStun();

        if (tankDeathVFX)
        {
            GameObject vfx = Instantiate(
                tankDeathVFX,
                transform.position + Vector3.up * 1.5f,
                Quaternion.identity
            );
            Destroy(vfx, 1.2f);
        }

        if (deathSound)
            AudioManager.Instance.PlaySound3D(deathSound, transform.position);

        animator?.SetBool(PARAM_ISDEAD, true);

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        base.Die();
    }

    protected override void OnDeathDestroyed()
    {
        Destroy(gameObject, 2.5f);
    }

    // ==========================
    // PLAYER CONTACT
    // ==========================
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
