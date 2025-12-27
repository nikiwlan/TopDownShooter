// BossBeetle.cs
using UnityEngine;

public class BossBeetle : EnemyBase
{
    [Header("Colliders / Stop")]
    [SerializeField] internal Collider bossBodyCollider;
    [SerializeField] internal Collider playerBodyCollider;
    [SerializeField] internal float stopPadding = 0.2f;

    [Header("Body Hit (Phase 0)")]
    public AudioClip bodyHitSound;
    [Range(0f, 1f)] public float bodyHitVolume = 1f;

    [Header("Health / Phases")]
    public int maxHealth = 30;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 12f;
    [SerializeField] internal float runStartRange = 8f;
    public LayerMask wallLayer;

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

    [Header("Phase 2 Movement")]
    public float walkSpeedPhase2 = 4f;

    [Header("Rage")]
    [SerializeField] internal float rageDuration = 2f;
    [SerializeField] internal bool immuneDuringRage = true;

    [Header("Attack Origins (per phase)")]
    [SerializeField] internal Transform attackOrigin1;
    [SerializeField] internal Transform attackOrigin2;
    [SerializeField] internal Transform attackOrigin3;

    [Header("Attack Settings (Phase 0/1/2)")]
    [SerializeField] internal float attackRange1 = 0.2f;
    [SerializeField] internal float attackRange2 = 0.2f;
    [SerializeField] internal float attackRange3 = 0.2f;

    [SerializeField] internal float attackDuration1 = 1.2f;
    [SerializeField] internal float attackDuration2 = 1.2f;
    [SerializeField] internal float attackDuration3 = 5f;

    [SerializeField] internal float attackCooldown1 = 1.5f;
    [SerializeField] internal float attackCooldown2 = 1.3f;
    [SerializeField] internal float attackCooldown3 = 1.1f;

    [Header("Global Recovery (prevents chaining)")]
    [SerializeField] internal float actionRecovery = 1.0f;

    [Header("Animation")]
    [SerializeField] internal Animator animator;

    [Header("VFX")]
    public GameObject tankHitVFX;
    public GameObject tankDeathVFX;

    [Header("Audio Clips")]
    public AudioClip attackHitSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    [Header("Player Contact Damage")]
    [SerializeField] private float _contactCooldown = 0.4f;
    private float _nextHitTime;

    internal BossBeetleContext Ctx { get; private set; }

    private IBossBeetlePhase _phase0;
    private IBossBeetlePhase _phase1;
    private IBossBeetlePhase _phase2;
    private IBossBeetlePhase _currentPhase;

    // Animator parameter names
    internal const string PARAM_PHASE = "Phase";        // int
    internal const string PARAM_CLOSE = "CloseToPlayer";// (optional) bool
    internal const string PARAM_ISRUN = "IsRunning";    // bool
    internal const string PARAM_SPEED = "Speed";        // (optional) float

    // Triggers (du hast gesagt: alle außer IsRunning sind Trigger)
    internal const string TRIG_ATTACK = "IsAttacking";
    internal const string TRIG_SPECIAL = "SpecialHit";
    internal const string TRIG_RAGE = "Rage";
    internal const string TRIG_GETHIT = "GetsHit";
    internal const string TRIG_DIE = "Die";

    // Internal guard, weil Trigger nicht abfragbar sind wie Bool
    private bool _isDead;

    // Optional parameter existence checks (verhindert Warnings & "es passiert nichts")
    private bool _hasCloseParam;
    private bool _hasSpeedParam;

    protected override void Start()
    {
        base.Start();

        pointsOnKill = 500;
        health = maxHealth;

        if (!animator) animator = GetComponentInChildren<Animator>();

        // Cache: existieren diese Animator-Parameter überhaupt?
        CacheAnimatorParams();

        var playerTransform = player ? player.transform : null;

        if (!bossBodyCollider) bossBodyCollider = GetComponentInChildren<Collider>();
        if (!playerBodyCollider && playerTransform) playerBodyCollider = playerTransform.GetComponentInChildren<Collider>();

        if (!attackOrigin1) attackOrigin1 = transform;
        if (!attackOrigin2) attackOrigin2 = transform;
        if (!attackOrigin3) attackOrigin3 = transform;

        if (!stunIconAnchor) stunIconAnchor = transform;

        Ctx = new BossBeetleContext(this, playerTransform);

        _phase0 = new BossBeetlePhase0(Ctx);
        _phase1 = new BossBeetlePhase1(Ctx);
        _phase2 = new BossBeetlePhase2(Ctx);

        int startPhase = GetPhase();
        animator.SetInteger(PARAM_PHASE, startPhase);
        SwitchToPhase(startPhase, triggerRage: false);
        Ctx.ApplyAnimatorRunFlag();
    }

    private void CacheAnimatorParams()
    {
        _hasCloseParam = HasAnimatorParam(animator, PARAM_CLOSE, AnimatorControllerParameterType.Bool);
        _hasSpeedParam = HasAnimatorParam(animator, PARAM_SPEED, AnimatorControllerParameterType.Float);
    }

    private static bool HasAnimatorParam(Animator anim, string name, AnimatorControllerParameterType type)
    {
        if (!anim) return false;
        foreach (var p in anim.parameters)
        {
            if (p.name == name && p.type == type) return true;
        }
        return false;
    }

    private void Update()
    {
        if (!Ctx.IsValid) return;
        if (_isDead) return;

        UpdatePhaseAndAnimator();

        bool closeForRun = Vector3.Distance(transform.position, Ctx.PlayerTransform.position) <= runStartRange;

        // Nur setzen, wenn der Bool-Parameter wirklich existiert
        if (_hasCloseParam)
            animator.SetBool(PARAM_CLOSE, closeForRun);

        if (Ctx.TickCoreGates()) return;

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
        SwitchToPhase(newPhase, triggerRage: true);
    }

    private int GetPhase()
    {
        return (health > 20) ? 0 : (health > 10) ? 1 : 2;
    }

    public void OnRunningCancelHitboxTriggered(Collider playerCollider)
    {
        if (!Ctx.IsRunning) return;
        if (Ctx.IsRaging || Ctx.IsStunned) return;
        if (playerCollider == null || !playerCollider.CompareTag("Player")) return;

        Ctx.StopRunAndCooldown(runCooldownAfterRun);

        // Nur setzen, wenn Speed existiert (sonst gibt's Animator-Warnings)
        if (_hasSpeedParam)
            animator.SetFloat(PARAM_SPEED, 0f);

        Ctx.ApplyAnimatorRunFlag();
    }

    public void OnHeadHit(int damage, Vector3 hitDir, Vector3 hitPoint)
    {
        _currentPhase?.OnHeadHit(damage, hitDir, hitPoint);
    }

    public void OnBodyHit(Vector3 hitPoint)
    {
        if (bodyHitSound)
            AudioManager.Instance.PlaySound3D(bodyHitSound, hitPoint, bodyHitVolume);
    }

    public override void TakeDamage(int amount, Vector3 hitDir, Vector3 hitPoint = default)
    {

        if (health <= 0) return;
        if (_isDead) return;

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


            //animator.ResetTrigger(TRIG_GETHIT);
            //animator.SetTrigger(TRIG_GETHIT);
       

        if (health <= 0)
            Die();
    }

    protected override void Die()
    {
        if (_isDead) return;
        _isDead = true;

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

        animator?.ResetTrigger(TRIG_GETHIT);
        animator?.ResetTrigger(TRIG_ATTACK);
        animator?.ResetTrigger(TRIG_SPECIAL);
        animator?.ResetTrigger(TRIG_RAGE);
        animator?.SetTrigger(TRIG_DIE);

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        base.Die();
    }

    protected override void OnDeathDestroyed()
    {
        Destroy(gameObject, 4.6f);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (_isDead) return;

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
