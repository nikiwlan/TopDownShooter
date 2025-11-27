using UnityEngine;

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
    [Range(0f, 1f)] public float shootVolume = 0.7f;   // ⭐ Lautstärke einstellbar!

    public bool isFrozen = false;

    private float nextFireTime = 0f;

    private Coroutine fireRateRoutine;
    private float baseFireRate;
    private float boostedFireRate;
    private float boostTimeLeft = 0f;

    void Start()
    {
        baseFireRate = fireRate;
        boostedFireRate = fireRate / 2f;
    }

    void Update()
    {
        if (isFrozen) return;

        AimAtMouse3D();
        HandleShooting();

        if (boostTimeLeft > 0f)
            boostTimeLeft -= Time.deltaTime;
    }

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

                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));

                if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.useGravity = false;
                    rb.velocity = dir * bulletSpeed;
                }

                Destroy(bullet, bulletLifetime);

                if (animator != null)
                {
                    animator.ResetTrigger("Shoot");
                    animator.SetTrigger("Shoot");
                }

                // ⭐ Schuss über AudioManager, mit einstellbarer Lautstärke
                if (shootSound != null)
                    AudioManager.Instance.PlaySound2D(shootSound, shootVolume);
            }
        }
    }

    public void ApplyFireRateBoost(float duration)
    {
        boostTimeLeft = Mathf.Min(boostTimeLeft + duration, 5f);

        if (fireRateRoutine == null)
            fireRateRoutine = StartCoroutine(FireRateBoostRoutine());
    }

    private System.Collections.IEnumerator FireRateBoostRoutine()
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
