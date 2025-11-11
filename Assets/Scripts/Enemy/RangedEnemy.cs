using System.Collections;
using UnityEngine;

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

    private Transform playerTransform;
    private Animator animator;

    // interne Zustände
    private float _baseSpeed;
    private bool _isSlowed;
    private float _slowEndTime;
    private float _shootTimer;
    private float _aimTimer; // kleine Zielverzögerung
    private Renderer _rend;
    private Color _origColor;
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
            animator.SetBool("IsFiring", false);
        }

        _baseSpeed = moveSpeed;
        _rend = GetComponentInChildren<Renderer>();
        if (_rend) _origColor = _rend.material.color;
    }

    void Update()
    {
        if (_didDie) return;
        if (!playerTransform) return;

        // TimeSlow prüfen
        if (_isSlowed && Time.time >= _slowEndTime)
        {
            _isSlowed = false;
            moveSpeed = _baseSpeed;
            if (_rend) _rend.material.color = _origColor;
        }

        _shootTimer -= Time.deltaTime;

        // Richtung & Bewegung
        Vector3 dir = (playerTransform.position - transform.position);
        dir.y = 0f;
        var n = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        float distance = dir.magnitude;

        // Animator-Parameter aktuell halten
        if (animator)
        {
            animator.SetFloat("Speed", distance > approachDistance ? 1f : 0f);
        }

        // Gegner schaut immer zum Spieler
        if (n != Vector3.zero)
        {
            var targetRot = Quaternion.LookRotation(n);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
        }

        Debug.DrawRay(transform.position + Vector3.up * 0.25f, n * 1.0f, Color.magenta);

        // Lauf-/Schießlogik
        if (distance > approachDistance)
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
            // Stoppt und zielt erst kurz
            _aimTimer += Time.deltaTime;

            if (animator)
                animator.SetFloat("Speed", 0f);

            if (_aimTimer >= 0.25f) // nach 0.3s beginnt er zu schießen
            {
                if (animator)
                    animator.SetBool("IsFiring", true);

                if (_shootTimer <= 0f && distance <= attackRange)
                {
                    // starte Schuss mit Delay für visuelles Timing
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

    private IEnumerator ShootWithDelay(Vector3 dir, float delay)
    {
        yield return new WaitForSeconds(delay);
        Shoot(dir);
    }

    private void Shoot(Vector3 dir)
    {
        if (!projectilePrefab) return;

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (muzzle != null)
        {
            spawnPos = muzzle.position;
            spawnRot = muzzle.rotation;
        }
        else
        {
            spawnPos = transform.position + dir * 1.0f + Vector3.up * 1.0f;
            spawnRot = Quaternion.LookRotation(dir);
        }

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

        if (proj.TryGetComponent(out ProjectileEnemy projectile))
            projectile.Init(dir);

        Debug.DrawRay(spawnPos, dir * 2f, Color.yellow, 1f);
    }

    public void ApplyTimeSlow(float duration, float factor)
    {
        _isSlowed = true;
        _slowEndTime = Mathf.Max(_slowEndTime, Time.time + duration);
        moveSpeed = _baseSpeed * factor;
        if (_rend) _rend.material.color = Color.cyan;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player") && !_didDie)
        {
            Die();
        }
    }

    protected override void Die()
    {
        if (_didDie) return;
        _didDie = true;

        if (animator != null)
        {
            animator.SetTrigger("Die");
            animator.SetBool("IsFiring", false);
            animator.SetFloat("Speed", 0f);
        }

        moveSpeed = 0f;
        Destroy(gameObject, 2.5f);

        base.Die();
    }
}
