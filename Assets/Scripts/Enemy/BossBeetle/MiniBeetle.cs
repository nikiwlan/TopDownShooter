using UnityEngine;

public class MiniBeetle : EnemyBase, ITimeSlowable
{
    [Header("Mini Beetle Stats")]
    public float moveSpeed = 6f;
    public float attackRange = 1.5f; // Wann er zuschlägt
    public float attackDamageDelay = 0.5f; // Wann der Schaden kommt (Animation timing)

    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip dieSound;

    private Transform playerTransform;
    private Animator animator;

    private bool isAttacking = false;
    private bool isDead = false;
    private float baseSpeed;

    // Time Slow Variablen
    private bool isSlowed = false;
    private float slowEndTime;

    // Animator Parameter Namen (Pass diese an deinen neuen Controller an!)
    private const string ANIM_ATTACK = "Attack";
    private const string ANIM_DIE = "Die";

    protected override void Start()
    {
        base.Start(); // Setzt health, pointsOnKill etc. aus EnemyBase

        // Deine gewünschten Stats setzen
        pointsOnKill = 10;
        health = 1; // Oder wie viel er haben soll
        baseSpeed = moveSpeed;

        playerTransform = player?.transform;
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        HandleTimeSlow();

        // 1. Forced Move (Rückstoß von Wänden)
        if (HasForcedMove)
        {
            HandleForcedMove();
            return;
        }

        // 2. Attack Logik
        if (isAttacking) return; // Wenn er schlägt, bewegt er sich nicht

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= attackRange)
        {
            StartCoroutine(AttackRoutine());
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    private void MoveTowardsPlayer()
    {
        if (moveSpeed <= 0) return;

        Vector3 dir = (playerTransform.position - transform.position).normalized;
        dir.y = 0; // Nicht in den Boden/Luft laufen

        // Bewegung
        transform.position += dir * moveSpeed * Time.deltaTime;

        // Rotation
        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 10f * Time.deltaTime);
        }
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // Stoppen
        float oldSpeed = moveSpeed;
        moveSpeed = 0f;

        // Animation
        if (animator) animator.SetTrigger(ANIM_ATTACK);

        // Warten bis zum "Hit" Moment
        yield return new WaitForSeconds(attackDamageDelay);

        // Schaden prüfen (ist Spieler noch in Reichweite?)
        if (player != null && Vector3.Distance(transform.position, player.transform.position) <= attackRange + 0.5f)
        {
            if (attackSound) AudioManager.Instance.PlaySound3D(attackSound, transform.position);
            player.TakeDamage(1);
        }

        // Warten bis Animation fertig (kurz geraten, ca 1s gesamtlänge)
        yield return new WaitForSeconds(0.5f);

        // Weiterlaufen
        moveSpeed = baseSpeed; // Reset auf normalen Speed
        isAttacking = false;
    }

    // --- Override von EnemyBase ---

    // Wenn er stirbt (durch Schuss)
    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator) animator.SetTrigger(ANIM_DIE);
        if (dieSound) AudioManager.Instance.PlaySound3D(dieSound, transform.position);

        DisableAllColliders();
        DisablePhysics();

        // Score und Destroy passiert im base.Die()
        base.Die();
    }

    // --- Helper für Forced Move ---
    private void HandleForcedMove()
    {
        Vector3 dir = ForcedMoveDirection;
        dir.y = 0;
        transform.position += dir * baseSpeed * Time.deltaTime;
        ConsumeForcedMove(baseSpeed * Time.deltaTime);
    }

    // --- Time Slow Interface ---
    public void ApplyTimeSlow(float duration, float factor)
    {
        isSlowed = true;
        slowEndTime = Time.time + duration;
        moveSpeed = baseSpeed * factor;
        SetColorAll(Color.cyan);
    }

    private void HandleTimeSlow()
    {
        if (isSlowed && Time.time >= slowEndTime)
        {
            isSlowed = false;
            moveSpeed = baseSpeed;
            ResetColorAll();
        }
    }
}