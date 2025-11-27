using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int health = 3;
    public int pointsOnKill = 10;

    [Header("Debug")]
    public bool debug = true;

    [Header("VFX")]
    public GameObject bloodHitVFX;   // Einfaches Blutbild bei Treffer

    [HideInInspector] public PlayerHealth player;

    private Collider[] allColliders;
    private Rigidbody rb;

    // Renderer-Cache für TimeSlow-Farbe
    protected Renderer[] allRenderers;
    protected Color[] originalColors;

    public Transform Root => transform;

    protected virtual void Awake()
    {
        allColliders = GetComponentsInChildren<Collider>(true);
        rb = GetComponent<Rigidbody>();

        allRenderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[allRenderers.Length];

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i].material.HasProperty("_Color"))
                originalColors[i] = allRenderers[i].material.color;
        }

        if (debug)
        {
            Debug.Log($"[{name}] Renderer Count: {allRenderers.Length}");
            Debug.Log($"[{name}] Collider Count: {allColliders.Length}");
            Debug.Log($"[{name}] Rigidbody vorhanden: {rb != null}");
        }
    }

    protected virtual void Start()
    {
        var pObj = GameObject.FindGameObjectWithTag("Player");
        player = pObj ? pObj.GetComponent<PlayerHealth>() : null;

        if (debug)
            Debug.Log($"[{name}] Player found: {(player ? "YES" : "NO")}");
    }

    // ---------------- COLOR HANDLING ----------------
    public void SetColorAll(Color c)
    {
        foreach (var r in allRenderers)
        {
            if (r.material.HasProperty("_Color"))
                r.material.color = c;
        }
    }

    public void ResetColorAll()
    {
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i].material.HasProperty("_Color"))
                allRenderers[i].material.color = originalColors[i];
        }
    }

    // Alte Version bleibt bestehen für Kompatibilität
    public virtual void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector3.forward, transform.position);
    }


    // Neue Version mit Treffer-Richtung
    public virtual void TakeDamage(int amount, Vector3 hitDir, Vector3 hitPoint = default)
    {
        if (hitPoint == default)
            hitPoint = transform.position;

        health -= amount;

        if (!(this is TankEnemy))
            SpawnBloodVFX(hitDir, hitPoint);

        if (debug)
            Debug.Log($"[{name}] Schaden: {amount} → verbleibend {health}");

        if (health <= 0)
            Die();
    }



    // Blut-Effekt hinter dem Hitpoint und höher auf Y
    protected void SpawnBloodVFX(Vector3 hitDir, Vector3 hitPoint)
    {
        if (bloodHitVFX == null)
            return;

        // Richtung der Kugel normalisieren
        Vector3 dir = hitDir.normalized;

        // Basis ist die Mitte des Gegners, nicht der Hitpoint
        // → so ist das Blut immer "auf der Rückseite" des Gegners
        // Collider-Größe erkennen
        float depthSize = 0.5f; // fallback

        if (TryGetComponent<Collider>(out Collider col))
        {
            depthSize = col.bounds.extents.z;
        }

        // Abstand dynamisch abhängig von Gegnergröße
        float offsetDistance = depthSize * 0.4f; // 20% weiter nach hinten

        Vector3 spawnPos =
            transform.position
            + Vector3.up * 0.9f
            + dir * offsetDistance;


        float randomRot = Random.Range(0f, 360f);

        GameObject vfx = Instantiate(
            bloodHitVFX,
            spawnPos,
            Quaternion.Euler(90f, randomRot, 0f)
        );

        Destroy(vfx, 0.4f);
    }




    protected virtual void Die()
    {
        DisableAllColliders();
        DisablePhysics();

        int multiplier = ScoreManager.Instance != null
            ? ScoreManager.Instance.scoreMultiplier
            : 1;

        int finalPoints = pointsOnKill * multiplier;

        ScoreManager.Instance?.AddScore(pointsOnKill);

        ScorePopupManager.Instance?.SpawnPopup(
            finalPoints,
            transform.position + Vector3.up * 1f
        );

        if (debug)
            Debug.Log($"[{name}] gestorben → Base {pointsOnKill}, Final {finalPoints}");

        OnDeathDestroyed();
    }

    protected virtual void OnDeathDestroyed()
    {
        Destroy(gameObject, 2f);
    }

    protected void DisableAllColliders()
    {
        foreach (var c in allColliders)
            c.enabled = false;
    }

    protected void DisablePhysics()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
    }
}

