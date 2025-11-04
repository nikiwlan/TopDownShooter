using UnityEngine;
using System.Collections;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;   // Projektil-Prefab
    public Transform firePoint;       // Schussposition
    public float fireRate = 0.25f;    // Zeit zwischen Schüssen
    public float bulletSpeed = 40f;   // Geschwindigkeit der Kugel
    public float bulletLifetime = 5f; // Sekunden bis zum Auto-Despawn

    [Header("Animation")]
    [SerializeField] private Animator animator; // Animator des RobotVisual

    private float nextFireTime = 0f;

    // --- FireRate Boost Variablen ---
    private Coroutine fireRateRoutine;
    private float baseFireRate;
    private float boostedFireRate;
    private float boostTimeLeft = 0f;

    void Start()
    {
        baseFireRate = fireRate;
        boostedFireRate = fireRate / 2f; // doppelt so schnell
    }

    void Update()
    {
        AimAtMouse3D();
        HandleShooting();

        if (boostTimeLeft > 0f)
            boostTimeLeft -= Time.deltaTime;
    }

    // 🎯 Visuelles Zielen
    void AimAtMouse3D()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 hitPoint = ray.GetPoint(rayDistance);
            Vector3 dir = (hitPoint - transform.position).normalized;
            dir.y = 0f;

            if (animator != null && dir.sqrMagnitude > 0.001f)
            {
                Transform robotVisual = animator.transform;
                Quaternion lookRotation = Quaternion.LookRotation(dir, Vector3.up);
                robotVisual.rotation = Quaternion.Slerp(robotVisual.rotation, lookRotation, 0.25f);
            }
        }
    }

    // 🔫 Schießen + Projektilrichtung zur Maus
    void HandleShooting()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float rayDistance))
            {
                Vector3 target = ray.GetPoint(rayDistance);
                Vector3 shootDir = (target - firePoint.position);
                shootDir.y = 0f;
                if (shootDir.sqrMagnitude < 0.01f)
                    shootDir = firePoint.forward;
                shootDir.Normalize();

                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDir));
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.velocity = shootDir * bulletSpeed;
                }
                Destroy(bullet, bulletLifetime);

                if (animator != null)
                {
                    animator.ResetTrigger("Shoot");
                    animator.SetTrigger("Shoot");
                    Debug.Log("[DEBUG] Shoot Trigger gesetzt auf Animator: " + animator.name);
                }
            }
        }
    }

    // --- FireRate Boost ---
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
