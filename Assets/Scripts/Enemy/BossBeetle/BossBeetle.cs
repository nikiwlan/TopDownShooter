// BossBeetle.cs
using UnityEngine;

public class BossBeetle : EnemyBase
{
    [Header("Colliders / Stop")]
    [SerializeField] internal Collider bossBodyCollider;
    [SerializeField] internal Collider playerBodyCollider;
    [SerializeField] internal float stopPadding = 0.2f;

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
    public GameObject headHitVFX;
    public GameObject beetleDeathVFX;

    [Header("Attack Audio")]
    [SerializeField] internal AudioClip normalAttackSound;
    [Range(0f, 1f)][SerializeField] internal float normalAttackVolume = 1f;

    [SerializeField] internal AudioClip specialAttackSound;
    [Range(0f, 1f)][SerializeField] internal float specialAttackVolume = 1f;

    // ==========================
    // NEW: Movement Audio
    // ==========================
    [Header("Movement Audio")]
    [SerializeField] internal AudioClip runSound;                 // one-shot on run start
    [Range(0f, 1f)][SerializeField] internal float runVolume = 1f;

    [SerializeField] internal AudioClip walkLoopSound;            // loop while walking
    [Range(0f, 1f)][SerializeField] internal float walkLoopVolume = 1f;

    [SerializeField] internal AudioClip wallCollisionSound;       // one-shot on wall crash
    [Range(0f, 1f)][SerializeField] internal float wallCollisionVolume = 1f;

    [Header("Other Audio")]
    public AudioClip deathSound;

    [Header("Hit Audio")]
    [SerializeField] private AudioClip[] headHitSounds = new AudioClip[4];
    [Range(0f, 1f)][SerializeField] private float headHitVolume = 1f;

    [Header("Body Hit (Phase 0)")]
    public AudioClip bodyHit; // renamed from bodyHitSound
    [Range(0f, 1f)] public float bodyHitVolume = 1f;

    [Header("Rage Audio")]
    public AudioClip rageSound;
    [Range(0f, 1f)] public float rageVolume = 1f;

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

    // Phases for weak Boss
    private bool _isWeakVariant = false;

    // ==========================
    // NEW: Walk loop source
    // ==========================
    private AudioSource _walkLoopSource;

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

        EnsureWalkLoopSource();

        Ctx = new BossBeetleContext(this, playerTransform);

        _phase0 = new BossBeetlePhase0(Ctx);
        _phase1 = new BossBeetlePhase1(Ctx);
        _phase2 = new BossBeetlePhase2(Ctx);

        int startPhase = GetPhase();
        animator.SetInteger(PARAM_PHASE, startPhase);
        SwitchToPhase(startPhase, triggerRage: false);
        Ctx.ApplyAnimatorRunFlag();
    }

    /// <summary>
    /// Wird vom WaveSpawner aufgerufen.
    /// startingHealth: 20 für schwachen Boss, 30 für starken.
    /// weakVariant: true = keine Phase 2, false = alle Phasen.
    /// </summary>
    public void ConfigureStats(int startingHealth, bool weakVariant)
    {
        // 1. Werte setzen
        maxHealth = startingHealth;
        health = startingHealth;
        _isWeakVariant = weakVariant;

        // 2. Status sofort aktualisieren
        // Damit der Animator und die Logik sofort wissen, in welcher Phase wir starten
        if (animator != null)
        {
            int phase = GetPhase();
            animator.SetInteger(PARAM_PHASE, phase);

            // Phase starten (ohne Rage-Schrei am Anfang, false)
            SwitchToPhase(phase, triggerRage: false);

            // Lauf-Animation Status updaten
            Ctx.ApplyAnimatorRunFlag();
        }
    }

    private void EnsureWalkLoopSource()
    {
        if (_walkLoopSource != null) return;

        _walkLoopSource = gameObject.AddComponent<AudioSource>();
        _walkLoopSource.playOnAwake = false;
        _walkLoopSource.loop = true;

        // 3D Sound (damit es zur Position passt)
        _walkLoopSource.spatialBlend = 1f;
        _walkLoopSource.rolloffMode = AudioRolloffMode.Linear;
        _walkLoopSource.minDistance = 1.5f;
        _walkLoopSource.maxDistance = 25f;
    }

    internal void StartWalkLoop()
    {
        if (!walkLoopSound) return;

        EnsureWalkLoopSource();

        // Clip setzen (falls gewechselt) + Volume updaten
        if (_walkLoopSource.clip != walkLoopSound)
            _walkLoopSource.clip = walkLoopSound;

        _walkLoopSource.volume = Mathf.Clamp01(walkLoopVolume);

        if (!_walkLoopSource.isPlaying)
            _walkLoopSource.Play();
    }

    internal void StopWalkLoop()
    {
        if (_walkLoopSource != null && _walkLoopSource.isPlaying)
            _walkLoopSource.Stop();
    }

    internal void PlayRunOneShot()
    {
        if (runSound)
            AudioManager.Instance.PlaySound3D(runSound, transform.position, runVolume);
    }

    internal void PlayWallCollisionOneShot()
    {
        if (wallCollisionSound)
            AudioManager.Instance.PlaySound3D(wallCollisionSound, transform.position, wallCollisionVolume);
    }

    private void CacheAnimatorParams()
    {
        _hasCloseParam = HasAnimatorParam(animator, PARAM_CLOSE, AnimatorControllerParameterType.Bool);
        _hasSpeedParam = HasAnimatorParam(animator, PARAM_SPEED, AnimatorControllerParameterType.Float);
    }

    private void PlayInvincibleBodyHit(Vector3 hitPoint)
    {
        Vector3 pos = (hitPoint == Vector3.zero) ? transform.position : hitPoint;
        if (bodyHit)
            AudioManager.Instance.PlaySound3D(bodyHit, pos, bodyHitVolume);
    }

    private void PlayRandomHeadHitSound(Vector3 position)
    {
        if (headHitSounds == null || headHitSounds.Length == 0) return;

        // Zähle nur gültige Clips (falls du weniger als 4 befüllt hast)
        int count = 0;
        for (int i = 0; i < headHitSounds.Length; i++)
            if (headHitSounds[i] != null) count++;

        if (count == 0) return;

        // Wähle zufällig aus den nicht-null Clips
        int pick = Random.Range(0, count);
        AudioClip chosen = null;

        for (int i = 0; i < headHitSounds.Length; i++)
        {
            var clip = headHitSounds[i];
            if (clip == null) continue;

            if (pick == 0) { chosen = clip; break; }
            pick--;
        }

        if (chosen)
            AudioManager.Instance.PlaySound3D(chosen, position, headHitVolume);
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
        if (_isWeakVariant)
        {
            // --- SCHWACHER BOSS (Max 20 HP) ---
            // HP 20 bis 11: Phase 0 (Laufen + Attacke 1)
            // HP 10 bis 0:  Phase 1 (Rennen + Attacke 2)
            // Er erreicht NIE Phase 2 (Special).

            return (health > 10) ? 0 : 1;
        }
        else
        {
            // --- NORMALER BOSS (Max 30 HP) ---
            // HP 30 bis 21: Phase 0
            // HP 20 bis 11: Phase 1
            // HP 10 bis 0:  Phase 2

            return (health > 20) ? 0 : (health > 10) ? 1 : 2;
        }
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
        if (bodyHit)
            AudioManager.Instance.PlaySound3D(bodyHit, hitPoint, bodyHitVolume);
    }

    public override void TakeDamage(int amount, Vector3 hitDir, Vector3 hitPoint = default)
    {
        if (health <= 0) return;
        if (_isDead) return;

        if (Ctx.IsRaging && immuneDuringRage)
        {
            Debug.Log("wtf");
            PlayInvincibleBodyHit(hitPoint);
            return;
        }


        // Headshot/Hit: random Sound am Trefferpunkt (oder Boss-Position fallback)
        Vector3 soundPos = (hitPoint == Vector3.zero) ? transform.position : hitPoint;
        PlayRandomHeadHitSound(soundPos);

        health -= amount;

        if (headHitVFX)
        {
            Vector3 spawnPos = (hitPoint == Vector3.zero)
                ? transform.position + Vector3.up * 1.5f
                : hitPoint;

            spawnPos += Vector3.up * 0.01f;

            if (Camera.main)
                spawnPos += -Camera.main.transform.forward * 0.02f;

            Quaternion rot = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);

            GameObject vfx = Instantiate(headHitVFX, spawnPos, rot);
            Destroy(vfx, 1.5f);
        }

        animator.ResetTrigger(TRIG_GETHIT);
        animator.SetTrigger(TRIG_GETHIT);

        if (health <= 0)
            Die();
    }

    public void ForceStartAttack2()
    {
        // Sicherheitschecks
        if (Ctx == null) return;
        if (Ctx.IsAttacking) return;

        // Phase prüfen (optional, aber sicher)
        int phase = animator.GetInteger("Phase");
        if (phase < 1) return;

        // Attacke 2 = Index 1 (0-basiert)
        Ctx.TryStartNormalAttack(1);
    }


    protected override void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Ctx.ForceStopAll();
        StopWalkLoop();

        if (beetleDeathVFX)
        {
            Vector3 spawnPos = transform.position;
            spawnPos += Vector3.up * 0.02f;

            Quaternion rot = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);

            GameObject vfx = Instantiate(beetleDeathVFX, spawnPos, rot);
            Destroy(vfx, 5f);
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
}
