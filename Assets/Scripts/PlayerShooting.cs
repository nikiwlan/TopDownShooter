using UnityEngine;
using System.Collections;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.25f;
    public float bulletSpeed = 40f;
    public float bulletLifetime = 5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Audio")]
    public AudioClip shootSound;

    private AudioSource sfxSource;      // 👉 von PlayerMovement
    private float nextFireTime = 0f;

    // --- FireRate Boost Variablen ---
    private Coroutine fireRateRoutine;
    private float baseFireRate;
    private float boostedFireRate;
    private float boostTimeLeft = 0f;

    void Start()
    {
        baseFireRate = fireRate;
        boostedFireRate = fireRate / 2f;

        // 👉 SFX-Source vom PlayerMovement holen
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            sfxSource = pm.GetSfxSource();
        }

        // Fallback, falls etwas Unerwartetes passiert
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }
    }

    void Update()
    {
        AimAtMouse3D();
        HandleShooting();

        if (boostTimeLeft > 0f)
            boostTimeLeft -= Time.deltaTime;
    }

    // -------------------------------------------------------
    // 🎯 Visuelles Zielen zur Maus
    // -------------------------------------------------------
    void AimAtMouse3D()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 hitPoint = ray.GetPoint(rayDistance);
            Vector3 lookDir = hitPoint - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude < 0.001f)
                return;

            if (animator != null)
            {
                Transform robotVisual = animator.transform;
                Quaternion lookRotation = Quaternion.LookRotation(lookDir);
                robotVisual.rotation = Quaternion.Slerp(robotVisual.rotation, lookRotation, 0.25f);
            }
        }
    }

    // -------------------------------------------------------
    // 🔫 Schießen
    // -------------------------------------------------------
    void HandleShooting()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 target = ray.GetPoint(dist);
                Vector3 dir = target - transform.position;
                dir.y = 0f;

                if (dir.magnitude < 0.5f)
                    dir = firePoint.forward;
                else
                    dir.Normalize();

                // --- Bullet spawnen ---
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));

                if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.useGravity = false;
                    rb.velocity = dir * bulletSpeed;
                }

                Destroy(bullet, bulletLifetime);

                // --- Animation ---
                if (animator != null)
                {
                    animator.ResetTrigger("Shoot");
                    animator.SetTrigger("Shoot");
                }

                // --- 🔊 Schuss-Sound ---
                if (shootSound != null && sfxSource != null)
                {
                    sfxSource.PlayOneShot(shootSound);
                }
            }
        }
    }

    // -------------------------------------------------------
    // 🔥 FireRate Boost
    // -------------------------------------------------------
    public void ApplyFireRateBoost(float duration)
    {
        boostTimeLeft = Mathf.Min(boostTimeLeft + duration, 5f);

        if (fireRateRoutine == null)
            fireRateRoutine = StartCoroutine(FireRateBoostRoutine());
    }

    private IEnumerator FireRateBoostRoutine()
    {
        fireRate = boostedFireRate;
        Debug.Log("[PlayerShooting] FireRate Boost aktiv!");

        while (boostTimeLeft > 0f)
            yield return null;

        fireRate = baseFireRate;
        fireRateRoutine = null;

        Debug.Log("[PlayerShooting] FireRate Boost vorbei.");
    }
}
