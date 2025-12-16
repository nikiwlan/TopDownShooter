using UnityEngine;
using System.Collections;

public class BossBeetle : EnemyBase
{
    [Header("Health / Phases")]
    public int maxHealth = 30;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 12f;
    [SerializeField] private float runStartRange = 8f;
    public LayerMask wallLayer;

    [Header("Attack Origins (per phase/attack)")]
    [SerializeField] private Transform attackOrigin1; // Phase 0 (HP > 20) -> Attack 1
    [SerializeField] private Transform attackOrigin2; // Phase 1 (10 < HP <= 20) -> Attack 2
    [SerializeField] private Transform attackOrigin3; // Phase 2 (HP <= 10) -> Attack 3

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

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("VFX")]
    public GameObject tankHitVFX;
    public GameObject tankDeathVFX;

    [Header("Audio Clips")]
    public AudioClip attackHitSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    private Transform playerTransform;

    private bool isAttacking = false;
    private float nextAttackTime = 0f;

    private float _nextHitTime;
    [SerializeField] private float _contactCooldown = 0.4f;

    protected override void Start()
    {
        base.Start();

        pointsOnKill = 500;
        health = maxHealth;

        playerTransform = player ? player.transform : null;
        if (!animator) animator = GetComponentInChildren<Animator>();

        // Fallbacks (falls du im Inspector nichts zuweist)
        if (!attackOrigin1) attackOrigin1 = transform;
        if (!attackOrigin2) attackOrigin2 = transform;
        if (!attackOrigin3) attackOrigin3 = transform;

        UpdatePhaseAndAnimator();
    }

    void Update()
    {
        if (!playerTransform || !animator) return;
        if (animator.GetBool("isDead")) return;

        UpdatePhaseAndAnimator();

        int phase = animator.GetInteger("Phase");

        Transform origin = GetOriginForPhase(phase);
        float attackRange = GetAttackRangeForPhase(phase);

        float distanceToOrigin = Vector3.Distance(origin.position, playerTransform.position);
        bool isInAttackRange = distanceToOrigin <= attackRange;

        // Run nur wenn Phase > 0 UND nah am Player
        float distanceToRoot = Vector3.Distance(transform.position, playerTransform.position);
        bool closeForRun = distanceToRoot <= runStartRange;
        animator.SetBool("CloseToPlayer", closeForRun);

        float moveSpeed =
            isAttacking ? walkSpeed :
            (phase > 0 && closeForRun ? runSpeed : walkSpeed);

        if (!isAttacking && !isInAttackRange)
        {
            MoveTowardsPlayer(moveSpeed);
            animator.SetFloat("Speed", moveSpeed);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }

        if (!isAttacking && isInAttackRange && Time.time >= nextAttackTime)
            StartCoroutine(AttackRoutine(phase));
    }

    private Transform GetOriginForPhase(int phase)
    {
        return phase switch
        {
            0 => attackOrigin1 ? attackOrigin1 : transform,
            1 => attackOrigin2 ? attackOrigin2 : transform,
            _ => attackOrigin3 ? attackOrigin3 : transform,
        };
    }

    private float GetAttackRangeForPhase(int phase)
    {
        return phase switch
        {
            0 => attackRange1,
            1 => attackRange2,
            _ => attackRange3,
        };
    }

    private int GetAttackDamageForPhase(int phase)
    {
        return phase switch
        {
            0 => attackDamage1,
            1 => attackDamage2,
            _ => attackDamage3,
        };
    }

    private float GetAttackDurationForPhase(int phase)
    {
        return phase switch
        {
            0 => attackDuration1,
            1 => attackDuration2,
            _ => attackDuration3,
        };
    }

    private float GetAttackCooldownForPhase(int phase)
    {
        return phase switch
        {
            0 => attackCooldown1,
            1 => attackCooldown2,
            _ => attackCooldown3,
        };
    }

    private void MoveTowardsPlayer(float speed)
    {
        Vector3 dir = (playerTransform.position - transform.position).normalized;
        dir.y = 0;

        if (!Physics.Raycast(transform.position, dir, speed * Time.deltaTime + 0.2f, wallLayer))
            transform.position += dir * speed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                0.2f
            );
        }
    }

    private void UpdatePhaseAndAnimator()
    {
        // Phase 0: HP > 20
        // Phase 1: 10 < HP <= 20
        // Phase 2: HP <= 10
        int phase = (health > 20) ? 0 : (health > 10) ? 1 : 2;
        animator.SetInteger("Phase", phase);
    }

    private IEnumerator AttackRoutine(int phase)
    {
        isAttacking = true;

        animator.SetTrigger("IsAttacking");

        Debug.Log($"[BossBeetle] ATTACK START | isAttacking={isAttacking} | Phase={phase}");

        float duration = GetAttackDurationForPhase(phase);

        yield return new WaitForSeconds(duration * 0.5f);

        if (attackHitSound)
            AudioManager.Instance.PlaySound3D(attackHitSound, transform.position);

        if (player != null)
            player.TakeDamage(GetAttackDamageForPhase(phase));

        yield return new WaitForSeconds(duration * 0.5f);

        isAttacking = false;

        Debug.Log($"[BossBeetle] ATTACK END | isAttacking={isAttacking} | Phase={phase}");

        nextAttackTime = Time.time + GetAttackCooldownForPhase(phase);
    }


    public override void TakeDamage(int amount, Vector3 hitDir, Vector3 hitPoint = default)
    {
        if (health <= 0) return;

        if (hitSound)
            AudioManager.Instance.PlaySound3D(hitSound, transform.position);

        health -= amount;

        if (debug)
            Debug.Log($"[BOSS] Schaden: {amount} → verbleibend {health}");

        if (health <= 0)
        {
            Die();
            return;
        }

        if (tankHitVFX != null)
        {
            GameObject vfx = Instantiate(
                tankHitVFX,
                transform.position + Vector3.up * 1.5f,
                Quaternion.Euler(90f, Random.Range(0f, 360f), 0f)
            );
            Destroy(vfx, 0.5f);
        }
    }

    protected override void Die()
    {
        if (tankDeathVFX != null)
        {
            GameObject vfx = Instantiate(
                tankDeathVFX,
                transform.position + Vector3.up * 1.5f,
                Quaternion.Euler(90f, Random.Range(0f, 360f), 0f)
            );
            Destroy(vfx, 1.2f);
        }

        if (deathSound)
            AudioManager.Instance.PlaySound3D(deathSound, transform.position);

        if (animator)
            animator.SetBool("isDead", true);

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        base.Die();
    }

    protected override void OnDeathDestroyed()
    {
        Destroy(gameObject, 2.5f);
    }

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
