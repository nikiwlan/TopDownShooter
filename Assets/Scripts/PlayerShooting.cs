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
    private float baseFireRate;          // Originalwert speichern
    private float boostedFireRate;       // Schnellere Schussrate
    private float boostTimeLeft = 0f;

    void Start()
    {
        baseFireRate = fireRate;
        boostedFireRate = fireRate / 2f; // doppelt so schnell (halbe Zeit)
    }

    void Update()
    {
        AimAtMouse();
        Shoot();

        // Optionaler Debug-Timer
        if (boostTimeLeft > 0f)
            boostTimeLeft -= Time.deltaTime;
    }

    void AimAtMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Shoot()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Instantiate(bulletPrefab, firePoint.position, transform.rotation);
        }
    }

    // --- FireRate Boost Logik ---
    public void ApplyFireRateBoost(float duration)
    {
        // addiere Zeit, aber begrenze auf max. 5 Sekunden
        boostTimeLeft = Mathf.Min(boostTimeLeft + duration, 5f);

        // wenn noch keine Routine läuft, starte sie
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
