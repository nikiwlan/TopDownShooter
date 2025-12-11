using UnityEngine;
using System.Collections;

public class RangedEnemy : EnemyBase, ITimeSlowable
{
    [Header("Movement & Attack Settings")]
    public float moveSpeed = 3f;
    public float attackRange = 8f;
    public float approachDistance = 6f;
    public float shootCooldown = 1.5f;
    public LayerMask wallLayer;
    public GameObject projectilePrefab;

    [Header("Weapon Settings")]
    public Transform muzzle;

    [Header("Laser Settings")]
    public LineRenderer laserRenderer;    // Referenz zum LineRenderer
    public float laserMaxDistance = 8f;  // wie weit der Laser maximal sichtbar ist
    public LayerMask playerLayer;         // Layer für Spieler (für Laser Raycast)

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip deathSound;

    private Transform playerTransform;
    private Animator animator;

    // TimeSlow
    private float baseSpeed;
    private bool isSlowed;
    private float slowEndTime;

    private float shootTimer;
    private float aimTimer;

    private bool killedByCollision = false;
    private bool didDie = false;

    // ----------------------------------------

    protected override void Start()
    {
        base.Start();
        pointsOnKill = 15;

        playerTransform = player ? player.transform : null;

        animator = GetComponentInChildren<Animator>();
        if (animator)
        {
            animator.Play("Walking");
            animator.SetFloat("Speed", 1f);
        }

        baseSpeed = moveSpeed;

        // LineRenderer initialisieren
        if (laserRenderer != null)
        {
            laserRenderer.positionCount = 2;
            laserRenderer.enabled = false;
        }
    }


    // ----------------------------------------

    void Update()
    {
        if (didDie) return;
        if (!playerTransform) return;

        HandleTimeSlow();

        shootTimer -= Time.deltaTime;

        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        Vector3 n = dir.normalized;
        float dist = dir.magnitude;

        if (n != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(n), 0.2f);

        // Bewegung & Animation
        if (dist > approachDistance)
        {
            float step = moveSpeed * Time.deltaTime;

            if (!Physics.Raycast(transform.position + Vector3.up * 0.25f, n, out RaycastHit hit, step + 0.2f, wallLayer))
                transform.position += n * step;

            aimTimer = 0f;

            if (animator)
            {
                animator.SetBool("IsFiring", false);
                animator.SetFloat("Speed", 1f);
            }

            // Laser aus wenn außerhalb der Reichweite
            if (laserRenderer != null)
                laserRenderer.enabled = false;
        }
        else
        {
            aimTimer += Time.deltaTime;

            if (animator)
                animator.SetFloat("Speed", 0f);

            if (aimTimer >= 0.25f)
            {
                if (animator)
                    animator.SetBool("IsFiring", true);

                // Laser an, aber nur wenn im Attack Range
                if (laserRenderer != null)
                {
                    laserRenderer.enabled = true;

                    Vector3 origin = muzzle.position;
                    Vector3 targetPos = origin + n * Mathf.Min(laserMaxDistance, dist);

                    RaycastHit hit;

                    // Raycast auf beide Layer gleichzeitig
                    LayerMask combinedMask = playerLayer | wallLayer;

                    if (Physics.Raycast(origin, n, out hit, laserMaxDistance, combinedMask))
                    {
                        targetPos = hit.point; // Laser stoppt genau dort, wo der Raycast trifft
                    }

                    laserRenderer.SetPosition(0, origin);
                    laserRenderer.SetPosition(1, targetPos);
                }


                if (shootTimer <= 0f)
                {
                    // Vor dem Schießen Laser aus
                    if (laserRenderer != null)
                        laserRenderer.enabled = false;

                    StartCoroutine(ShootWithDelay(n, 0.25f));
                    shootTimer = shootCooldown;
                }
            }
            else
            {
                if (animator)
                    animator.SetBool("IsFiring", false);

                if (laserRenderer != null)
                    laserRenderer.enabled = false;
            }
        }
    }


    // ----------------------------------------
    private IEnumerator ShootWithDelay(Vector3 dir, float delay)
    {
        yield return new WaitForSeconds(delay);
        Shoot(dir);
    }

    private void Shoot(Vector3 dir)
    {
        if (!projectilePrefab) return;

        Vector3 spawnPos = muzzle ? muzzle.position : transform.position + Vector3.up;
        Quaternion spawnRot = muzzle ? muzzle.rotation : Quaternion.LookRotation(dir);

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

        if (proj.TryGetComponent(out ProjectileEnemy projectile))
            projectile.Init(dir);

        // 3D SOUND via manager
        if (shootSound != null)
            EnemyShootManager.Instance?.RegisterShot(transform.position);
    }

    // ----------------------------------------
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player") && !didDie)
        {
            killedByCollision = true;
            player.TakeDamage(1);
            Die();
        }
    }

    protected override void Die()
    {
        if (didDie) return;
        didDie = true;

        if (animator)
        {
            animator.SetTrigger("Die");
            animator.SetBool("IsFiring", false);
        }

        moveSpeed = 0f;

        if (deathSound)
            AudioManager.Instance.PlaySound3D(deathSound, transform.position);

        if (!killedByCollision)
            base.Die();

        Destroy(gameObject, 2.5f);
    }

    // ----------------------------------------
    public void ApplyTimeSlow(float duration, float factor)
    {
        isSlowed = true;
        slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
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
