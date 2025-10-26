using UnityEngine;
using System.Collections;


public class PlayerShooting : MonoBehaviour
{
    public GameObject bulletPrefab;   // Referenz aufs Bullet
    public Transform firePoint;       // Position, an der Kugeln gespawnt werden
    public float fireRate = 0.25f;    // Schussrate (Sekunden zwischen Schüssen)
    private float nextFireTime = 0f;

    void Update()
    {
        AimAtMouse();
        Shoot();
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

    public IEnumerator TempFireRateBoost(float duration)
    {
        float originalRate = fireRate;
        fireRate /= 2f; // doppelt so schnell
        Debug.Log("[PlayerShooting] FireRate Boost aktiv!");

        yield return new WaitForSeconds(duration);

        fireRate = originalRate;
        Debug.Log("[PlayerShooting] FireRate Boost vorbei.");
    }

}
