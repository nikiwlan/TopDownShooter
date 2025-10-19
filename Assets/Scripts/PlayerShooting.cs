using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject bulletPrefab;   // Referenz aufs Bullet
    public Transform firePoint;       // Position, an der Kugeln gespawnt werden
    public float fireRate = 0.25f;    // Schussrate (Sekunden zwischen Sch�ssen)
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
}
