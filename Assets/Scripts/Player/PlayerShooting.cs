using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Base Stats")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float baseFireRate = 0.25f;
    public float bulletSpeed = 40f;
    public float bulletLifetime = 5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    public float baseAnimSpeed = 1.0f;

    [Header("Audio")]
    public AudioClip shootSound;
    [Range(0f, 1f)] public float shootVolume = 0.7f;

    // --- PAUSE VARIABLE (Wieder da!) ---
    public bool isFrozen = false;

    // --- INTERNE STATS ---
    private int fireRateLevel = 0;
    private bool isTempBoostActive = false;
    private float boostTimeLeft = 0f;
    private float nextFireTime = 0f;

    void Update()
    {
        // 1. Wenn Spiel pausiert ist -> Nix machen
        if (isFrozen) return;

        // 2. Temp Boost Timer verwalten
        if (isTempBoostActive)
        {
            boostTimeLeft -= Time.unscaledDeltaTime;
            if (boostTimeLeft <= 0) isTempBoostActive = false;
        }

        // 3. Multiplikator berechnen
        float currentMultiplier = CalculateMultiplier();

        // 4. Animation synchronisieren
        if (animator != null)
        {
            animator.SetFloat("ShootSpeedMult", baseAnimSpeed * currentMultiplier);
        }

        // 5. Schießen & Zielen
        HandleShooting(currentMultiplier);
        AimAtMouse3D();
    }

    // --- LOGIK ---
    float CalculateMultiplier()
    {
        float multiplier = 1.0f;

        // +10% pro Upgrade-Level
        multiplier += (fireRateLevel * 0.1f);

        // x2 wenn Boost aktiv
        if (isTempBoostActive) multiplier *= 2f;

        return multiplier;
    }

    void HandleShooting(float multiplier)
    {
        float currentDelay = baseFireRate / multiplier;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + currentDelay;
            FireBullet();

            if (animator != null)
            {
                animator.ResetTrigger("Shoot");
                animator.SetTrigger("Shoot");
            }
        }
    }

    // --- VON AUSSEN AUFRUFBAR ---

    public void ApplyFireRateBoost(float duration)
    {
        boostTimeLeft = duration;
        isTempBoostActive = true;
        Debug.Log("Boost aktiviert!");
    }

    public void UpgradeFireRate()
    {
        fireRateLevel++;
        Debug.Log($"Upgrade! Level: {fireRateLevel}");
    }

    // --- STANDARD ZEUG ---
    void FireBullet()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float dist))
        {
            Vector3 dir = ray.GetPoint(dist) - transform.position;
            dir.y = 0;
            if (dir.magnitude < 0.5f) dir = firePoint.forward; else dir.Normalize();

            GameObject b = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
            if (b.TryGetComponent<Bullet>(out var s)) s.Init(dir, bulletSpeed, bulletLifetime);
            else Destroy(b, bulletLifetime);

            if (shootSound) AudioManager.Instance.PlaySound2D(shootSound, shootVolume);
        }
    }

    void AimAtMouse3D()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 lookDir = ray.GetPoint(rayDistance) - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f && animator)
            {
                animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, Quaternion.LookRotation(lookDir), 0.25f);
            }
        }
    }
}