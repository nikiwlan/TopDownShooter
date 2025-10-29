using UnityEngine;
using System.Collections;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;   // Referenz aufs Bullet
    public Transform firePoint;       // Position, an der Kugeln gespawnt werden
    public float fireRate = 0.25f;    // Sekunden zwischen Schüssen

    private float nextFireTime = 0f;

    // --- FireRate Boost Variablen ---
    private Coroutine fireRateRoutine;
    private float baseFireRate;
    private float boostedFireRate;
    private float boostTimeLeft = 0f;

    void Start()
    {
        baseFireRate = fireRate;
        boostedFireRate = fireRate / 2f; // doppelt so schnell (halbe Zeit)
    }

    void Update()
    {
        AimAtMouse3D();
        Shoot();

        // Optionaler Debug-Timer
        if (boostTimeLeft > 0f)
            boostTimeLeft -= Time.deltaTime;
    }

    // 🔄 Angepasst: Maus-Zielen für 3D / Top-Down
    void AimAtMouse3D()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Y=0 Ebene
        float rayDistance;

        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 hitPoint = ray.GetPoint(rayDistance);
            Vector3 direction = (hitPoint - transform.position).normalized;
            direction.y = 0f; // Bleib in der XZ-Ebene

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = lookRotation;
            }
        }
    }

    void Shoot()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
    }

    // --- FireRate Boost Logik ---
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
        {
            yield return null;
        }

        fireRate = baseFireRate;
        fireRateRoutine = null;
        Debug.Log("[PlayerShooting] FireRate Boost vorbei.");
    }
}
