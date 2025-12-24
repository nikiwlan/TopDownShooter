// BossBeetle.cs
using UnityEngine;

public class BossBeetle : EnemyBase
{
    // ==========================
    // COLLIDERS / STOPPING
    // ==========================
    [Header("Colliders / Stop")]
    [SerializeField] internal Collider bossBodyCollider;   // z.B. Capsule am Boss
    [SerializeField] internal Collider playerBodyCollider; // Player Collider
    [SerializeField] internal float stopPadding = 0.2f;    // extra Abstand

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
    [SerializeField] internal float runStartRange = 8f;
    public LayerMask wallLayer;

    // ==========================
    // RUN (PHASE 1/2)
    // ==========================
    [Header("Run (Phase 1/2)")]
    [SerializeField] internal float runMaxTime = 1.2f;
    [SerializeField] internal float runStopDistance = 1.2f;
    [SerializeField] internal float runSteerStrength = 0.7f;
    [SerializeField] internal float runSteerNearMultiplier = 0.2f;
    [SerializeField] internal float runSteerFarMultiplier = 1.0f;
    [SerializeField] internal float runSteerDistanceRange = 8f;
    [SerializeField] internal float runWallCheckDistance = 0.9f;

    [Header("Run Cooldown (Phase 1/2)")]
    [SerializeField] internal float runCooldownAfterRun = 2.0f;
    [SerializeField] internal float runCooldownAfterStun = 2.0f;

    [Header("Wall Stun (Phase 1/2)")]
    [SerializeField] internal float wallStunDuration = 3f;
    [SerializeField] internal GameObject stunIconPrefab;
    [SerializeField] internal Transform stunIconAnchor;

    // ==========================
    // RAGE
    // ==========================
    [Header("Rage")]
    [SerializeField] internal float rageDuration = 2f;
    [SerializeField] internal bool immuneDuringRage = true;

    // ==========================
    // ATTACK ORIGINS
    // ==========================
    [Header("Attack Origins (per phase)")]
    [SerializeField] internal Transform attackOrigin1;
    [SerializeField] internal Transform attackOrigin2;
    [SerializeField] internal Transform attackOrigin3;

    // ==========================
    // ATTACK SETTINGS (Phase 0/1/2)
    // ==========================
    [Header("Attack Settings (Phase 0/1/2)")]
    [SerializeField] internal float attackRange1 = 2f;
    [SerializeField] internal float attackRange2 = 2f;
    [SerializeField] internal float attackRange3 = 2f;

    [SerializeField] internal int attackDamage1 = 1;
    [SerializeField] internal int attackDamage2 = 2;
    [SerializeField] internal int attackDamage3 = 3;

    [SerializeField] internal float attackDuration1 = 1.2f;
    [SerializeField] internal float attackDuration2 = 1.2f;
    [SerializeField] internal float attackDuration3 = 1.2f;

    [SerializeField] internal float attackCooldown1 = 1.5f;
    [SerializeField] internal float attackCooldown2 = 1.3f;
    [SerializeField] internal float attackCooldown3 = 1.1f;

    // ==========================
    // PHASE 2 - JUMP ATTACK (neu)
    // ==========================
    [Header("Phase 2: Jump Attack (neu)")]
    [SerializeField] internal float jumpCooldown = 3.0f;
    [SerializeField] internal float jumpDuration = 0.7f;       // Zeit für den Sprung
    [SerializeField] internal float jumpHeight = 2.0f;         // Arc Höhe
    [SerializeField] internal float jumpLandingRadius = 2.2f;  // Schaden bei Landung, wenn Player nah
    [SerializeField] internal int jumpLandingDamage = 2;

    // ==========================
    // ANIMATION / FX / AUDIO
    // ==========================
    [Header("Animation")]
    [SerializeField] internal Animator animator;

    [Header("VFX")]
    public GameObject tankHitVFX;
    public GameObject tankDeathVFX;

    [Header("Audio Clips")]
    public AudioClip attackHitSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    // ==========================
    // CONTACT DAMAGE
    // ==========================
    [Header("Player Contact Damage")]
    [SerializeField] private float _contactCooldown = 0.4f;
    private float _nextHitTime;

    // ==========================
    // INTERNAL (State Machine)
    // ==========================
    internal BossBeetleContext Ctx { get; private set; }

    private IBossBeetlePhase _phase0;
    private IBossBeetlePhase _phase1;
    private IBossBeetlePhase _phase2;
    private IBossBeetlePhase _currentPhase;

    // Animator parameter names (wie bei dir)
    internal const string PARAM_PHASE = "Phase";
    internal const string PARAM_CLOSE = "CloseToPlayer";
    internal const string PARAM_ISRUN = "IsRunning";
    internal const string PARAM_ISDEAD = "Die";
    internal const string TRIG_ATTACK = "IsAttacking";
    internal const string TRIG_RAGE = "Rage";

    protected override void Start()
    {
        base.Start();

        pointsOnKill = 500;
        health = maxHealth;

        if (!animator) animator = GetComponentInChildren<Animator>();

        var playerTransform = player ? player.transform : null;

        if (!bossBodyCollider) bossBodyCollider = GetComponentInChildren<Collider>();
        if (!playerBodyCollider && playerTransform) playerBodyCollider = playerTransform.GetComponentInChildren<Collider>();

        if (!attackOrigin1) attackOrigin1 = transform;
        if (!attackOrigin2) attackOrigin2 = transform;
        if (!attackOrigin3) attackOrigin3 = transform;

        if (!stunIconAnchor) stunIconAnchor = transform;

        // Context + Phasen erstellen
        Ctx = new BossBeetleContext(this, playerTransform);

        _phase0 = new BossBeetlePhase0(Ctx);
        _phase1 = new BossBeetlePhase1(Ctx);
        _phase2 = new BossBeetlePhase2(Ctx);

        // Init Phase OHNE Rage
        int startPhase = GetPhase();
        animator.SetInteger(PARAM_PHASE, startPhase);
        SwitchToPhase(startPhase, triggerRage: false);
        Ctx.ApplyAnimatorRunFlag();
    }

    private void Update()
    {
        if (!Ctx.IsValid) return;
        if (animator.GetBool(PARAM_ISDEAD)) return;

        // Phase + Animator zentral updaten (inkl. Rage beim Wechsel)
        UpdatePhaseAndAnimator();

        // CloseToPlayer Flag (wie bei dir)
        bool closeForRun = Vector3.Distance(transform.position, Ctx.PlayerTransform.position) <= runStartRange;
        animator.SetBool(PARAM_CLOSE, closeForRun);

        // Core gating (Stun/Rage/Attack blockiert)
        if (Ctx.TickCoreGates()) return;

        // Phase tickt die Logik
        _currentPhase?.Tick(closeForRun);
    }

    private void SwitchToPhase(int phase, bool triggerRage)
    {
        _currentPhase?.Exit();

        _currentPhase = phase switch
        {
            0 => _phase0,
            1 => _phase1,
            _ => _phase2
        };

        _currentPhase.Enter(triggerRage);
    }

    private void UpdatePhaseAndAnimator()
    {
        int newPhase = GetPhase();
        int oldPhase = animator.GetInteger(PARAM_PHASE);

        if (newPhase == oldPhase) return;

        animator.SetInteger(PARAM_PHASE, newPhase);

        // Rage Start (wie bei dir)
        SwitchToPhase(newPhase, triggerRage: true);
    }

    private int GetPhase()
    {
        return (health > 20) ? 0 : (health > 10) ? 1 : 2;
    }

    // ==========================
    // EVENTS / HITBOX CALLBACKS
    // ==========================
    public void OnRunningCancelHitboxTriggered(Collider playerCollider)
    {
        // identische Safety wie bei dir
        if (!Ctx.IsRunning) return;
        if (Ctx.IsRaging || Ctx.IsStunned) return;
        if (playerCollider == null || !playerCollider.CompareTag("Player")) return;

        Ctx.StopRunAndCooldown(runCooldownAfterRun);
        animator.SetFloat("Speed", 0f);
        Ctx.ApplyAnimatorRunFlag();
    }

    // HIT (called by bullet)
    public void OnHeadHit(int damage, Vector3 hitDir, Vector3 hitPoint)
    {
        _currentPhase?.OnHeadHit(damage, hitDir, hitPoint);
    }

    public void OnBodyHit(Vector3 hitPoint)
    {
        if (bodyHitSound)
            AudioManager.Instance.PlaySound3D(bodyHitSound, hitPoint, bodyHitVolume);
    }

    // ==========================
    // DAMAGE / DEATH (wie bei dir)
    // ==========================
    public override void TakeDamage(int amount, Vector3 hitDir, Vector3 hitPoint = default)
    {
        if (health <= 0) return;

        if (Ctx.IsRaging && immuneDuringRage)
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
            Die();
    }

    protected override void Die()
    {
        Ctx.ForceStopAll();

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
    // PLAYER CONTACT (wie bei dir)
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
