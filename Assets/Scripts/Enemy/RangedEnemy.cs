using UnityEngine;
using System.Collections;

public class RangedEnemy : EnemyBase, ITimeSlowable
{
    [Header("Gate Hit Sound")]
    public AudioClip gateHitSound;
    public float gateHitVolume = 1f;

    [Header("Movement & Attack Settings")]
    public float moveSpeed = 3f;
    public float attackRange = 8f;
    public float approachDistance = 6f;
    public float shootCooldown = 1.5f;
    public LayerMask wallLayer;
    public GameObject projectilePrefab;

    [Header("Weapon Settings")]
    public Transform muzzle;

    [Header("Audio")]
    public AudioClip shootSound;
    private AudioSource audioSource;

    private Transform playerTransform;
    private Animator animator;

    private float _baseSpeed;
    private bool _isSlowed;
    private float _slowEndTime;
    private Renderer _rend;
    private Color _origColor;

    private float _shootTimer;
    private float _aimTimer;

    private bool killedByCollision = false;
    private bool _didDie;

    protected override void Start()
    {
        base.Start();
        pointsOnKill = 15;

        playerTransform = player ? player.transform : null;

        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.Play("Walking");
            animator.SetFloat("Speed", 1f);
        }

        _baseSpeed = moveSpeed;

        _rend = GetComponentInChildren<SkinnedMeshRenderer>();
        if (_rend) _origColor = _rend.material.color;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }


    // ---------------- TIME SLOW ----------------
    public void ApplyTimeSlow(float duration, float factor)
    {
        _isSlowed = true;
        _slowEndTime = Mathf.Max(_slowEndTime, Time.time + duration);
        moveSpeed = _baseSpeed * factor;

        if (_rend) _rend.material.color = Color.cyan;
    }


    // ---------------- UPDATE ----------------
    void Update()
    {
        if (_didDie) return;
        if (!playerTransform) return;

        if (_isSlowed && Time.time >= _slowEndTime)
        {
            _isSlowed = false;
            moveSpeed = _baseSpeed;
            if (_rend) _rend.material.color = _origColor;
        }

        _shootTimer -= Time.deltaTime;

        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        Vector3 n = dir.normalized;
        float dist = dir.magnitude;

        if (n != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(n), 0.2f);

        if (dist > approachDistance)
        {
            float step = moveSpeed * Time.deltaTime;

            if (!Physics.Raycast(transform.position + Vector3.up * 0.25f, n, out RaycastHit hit, step + 0.2f, wallLayer))
                transform.position += n * step;

            _aimTimer = 0f;

            if (animator)
            {
                animator.SetBool("IsFiring", false);
                animator.SetFloat("Speed", 1f);
            }
        }
        else
        {
            _aimTimer += Time.deltaTime;

            if (animator)
                animator.SetFloat("Speed", 0f);

            if (_aimTimer >= 0.25f)
            {
                if (animator)
                    animator.SetBool("IsFiring", true);

                if (_shootTimer <= 0f)
                {
                    StartCoroutine(ShootWithDelay(n, 0.25f));
                    _shootTimer = shootCooldown;
                }
            }
            else
            {
                if (animator)
                    animator.SetBool("IsFiring", false);
            }
        }
    }


    // ---------------- SHOOTING ----------------
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

        if (shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }


    // ---------------- COLLISION ----------------
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        // Player collision (wie vorher)
        if (other.CompareTag("Player") && !_didDie)
        {
            killedByCollision = true;
            player.TakeDamage(1);
            Die();
            return;
        }

        // Gate collision → EXACT wie beim TankEnemy
        if (other.CompareTag("Gate"))
        {
            if (gateHitSound != null)
                audioSource.PlayOneShot(gateHitSound, gateHitVolume);
        }
    }


    // ---------------- DEATH ----------------
    protected override void Die()
    {
        if (_didDie) return;
        _didDie = true;

        if (animator)
        {
            animator.SetTrigger("Die");
            animator.SetBool("IsFiring", false);
        }

        moveSpeed = 0f;

        if (!killedByCollision)
            base.Die();

        Destroy(gameObject, 2.5f);
    }
}
