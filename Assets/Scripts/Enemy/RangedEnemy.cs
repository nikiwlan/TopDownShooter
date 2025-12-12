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
    public LineRenderer laserRenderer;       // LineRenderer auf dem LaserSight-Objekt
    public float laserMaxDistance = 25f;     // Reichweite des Lasers
    public LayerMask laserHitMask;           // z.B. Player + Wall
    public float targetHeightOffset = 0.5f;  // wie hoch am Spieler gezielt wird

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip deathSound;
    private AudioSource audioSource;

    private Transform playerTransform;
    private Animator animator;

    // Time Slow
    private float baseSpeed;
    private bool isSlowed;
    private float slowEndTime;

    // Attack logic
    private float shootTimer;
    private float aimTimer;

    private bool killedByCollision = false;
    private bool didDie = false;

    // Laser color protection (Laser soll NICHT cyan werden)
    private Color _laserStartColor = Color.red;
    private bool _laserColorCached = false;

    // ---------------- START ----------------
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

        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Laser vorbereiten
        if (laserRenderer != null)
        {
            laserRenderer.positionCount = 2;
            laserRenderer.enabled = false;
            CacheLaserColor();       // Originalfarbe merken (rot)
            RestoreLaserColor();     // sicherstellen, dass Laserfarbe korrekt gesetzt ist
        }
    }

    // ---------------- UPDATE ----------------
    void Update()
    {
        if (didDie) return;
        if (!playerTransform) return;

        // TimeSlow verarbeiten
        HandleTimeSlow();

        shootTimer -= Time.deltaTime;

        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        Vector3 n = dir.normalized;
        float dist = dir.magnitude;

        // Rotation
        if (n != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(n), 0.2f);

        // Bewegung / Angriff
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

            DisableLaser();
        }
        else
        {
            aimTimer += Time.deltaTime;

            if (animator)
                animator.SetFloat("Speed", 0f);

            // ab hier "zielt" er (wie vorher, ab 0.25s)
            if (aimTimer >= 0.25f)
            {
                if (animator)
                    animator.SetBool("IsFiring", true);

                // --- LASER AKTUALISIEREN ---
                UpdateLaser();

                if (shootTimer <= 0f)
                {
                    StartCoroutine(ShootWithDelay(n, 0.25f));
                    shootTimer = shootCooldown;
                }
            }
            else
            {
                if (animator)
                    animator.SetBool("IsFiring", false);

                DisableLaser();
            }
        }
    }

    // ---------------- SHOOTING ----------------
    private IEnumerator ShootWithDelay(Vector3 dir, float delay)
    {
        yield return new WaitForSeconds(delay);
        Shoot(dir);

        // damit der Laser beim nächsten Schuss wieder neu "aufgebaut" wird
        aimTimer = 0f;
    }

    private void Shoot(Vector3 dir)
    {
        if (!projectilePrefab) return;

        // Laser aus im Moment des Schusses
        DisableLaser();

        Vector3 spawnPos = muzzle ? muzzle.position : transform.position + Vector3.up;
        Quaternion spawnRot = muzzle ? muzzle.rotation : Quaternion.LookRotation(dir);

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

        if (proj.TryGetComponent(out ProjectileEnemy projectile))
            projectile.Init(dir);

        if (shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }

    // ---------------- LASER ----------------
    private void UpdateLaser()
    {
        if (laserRenderer == null || muzzle == null || playerTransform == null)
            return;

        // Laserfarbe immer erzwingen (damit TimeSlow ihn nicht blau/cyan macht)
        RestoreLaserColor();

        Vector3 origin = muzzle.position;

        // Zielpunkt am Spieler (mit Höhe), damit der Raycast NICHT unter dem Collider durchgeht
        Vector3 target = playerTransform.position + Vector3.up * targetHeightOffset;

        Vector3 d = (target - origin).normalized;
        if (d.sqrMagnitude < 0.0001f)
            d = transform.forward;

        float distance = laserMaxDistance;
        Vector3 end = origin + d * distance;

        // Raycast gegen Player + Wände
        if (laserHitMask.value != 0)
        {
            if (Physics.Raycast(origin, d, out RaycastHit hit, distance, laserHitMask, QueryTriggerInteraction.Collide))
                end = hit.point;
        }

        laserRenderer.enabled = true;
        laserRenderer.SetPosition(0, origin);
        laserRenderer.SetPosition(1, end);
    }

    private void DisableLaser()
    {
        if (laserRenderer != null && laserRenderer.enabled)
            laserRenderer.enabled = false;
    }

    private void CacheLaserColor()
    {
        if (laserRenderer == null) return;

        _laserStartColor = laserRenderer.startColor;
        _laserColorCached = true;
    }

    private void RestoreLaserColor()
    {
        if (laserRenderer == null) return;

        if (!_laserColorCached)
            CacheLaserColor();

        laserRenderer.startColor = _laserStartColor;
        laserRenderer.endColor = _laserStartColor;
    }

    // ---------------- COLLISION ----------------
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

    // ---------------- DEATH ----------------
    protected override void Die()
    {
        if (didDie) return;
        didDie = true;

        DisableLaser();

        if (animator)
        {
            animator.SetTrigger("Die");
            animator.SetBool("IsFiring", false);
        }

        moveSpeed = 0f;

        if (deathSound)
            audioSource.PlayOneShot(deathSound);

        if (!killedByCollision)
            base.Die();

        Destroy(gameObject, 2.5f);
    }

    // ---------------- TIME SLOW ----------------
    public void ApplyTimeSlow(float duration, float factor)
    {
        isSlowed = true;
        slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
        moveSpeed = baseSpeed * factor;

        // Gegner cyan färben (wie vorher)
        SetColorAll(Color.cyan);

        // ABER: Laser soll rot bleiben
        RestoreLaserColor();
    }

    private void HandleTimeSlow()
    {
        if (isSlowed && Time.time >= slowEndTime)
        {
            isSlowed = false;
            moveSpeed = baseSpeed;
            ResetColorAll();

            // Laser wieder sicher auf rot
            RestoreLaserColor();
        }
    }
}
