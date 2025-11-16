using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int health = 3;
    public int pointsOnKill = 10;

    [Header("Debug")]
    public bool debug = true;

    [HideInInspector] public PlayerHealth player;

    private Collider[] allColliders;
    private Rigidbody rb;

    // ----------------------------------------------------------
    // START / AWAKE
    // ----------------------------------------------------------
    protected virtual void Awake()
    {
        allColliders = GetComponentsInChildren<Collider>(true);
        rb = GetComponent<Rigidbody>();

        if (debug)
        {
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

    // ----------------------------------------------------------
    // DAMAGE
    // ----------------------------------------------------------
    public virtual void TakeDamage(int amount)
    {
        health -= amount;

        if (debug)
            Debug.Log($"[{name}] Schaden: {amount} → verbleibend {health}");

        if (health <= 0)
            Die();
    }

    // ----------------------------------------------------------
    // DEATH (ZENTRAL FÜR SCORE & POPUP)
    // ----------------------------------------------------------
    protected virtual void Die()
    {
        DisableAllColliders();   // ✅ Gegner kollidiert ab jetzt mit NICHTS mehr
        DisablePhysics();        // ✅ Physik abschalten (optional)

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

        // Gegner wird NICHT sofort zerstört!
        OnDeathDestroyed();
    }

    // ----------------------------------------------------------
    // SEPARATE ZERSTÖRUNG (damit Animation ablaufen kann)
    // ----------------------------------------------------------
    protected virtual void OnDeathDestroyed()
    {
        // wird von FastEnemy, TankEnemy, RangedEnemy überschrieben
        Destroy(gameObject, 2f);
    }

    // ----------------------------------------------------------
    // HILFE: Collider / Physik ausschalten
    // ----------------------------------------------------------
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

    // ----------------------------------------------------------
    // DEBUG TRIGGER
    // ----------------------------------------------------------
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!debug) return;

        Debug.Log($"[{name}] Trigger → {other.name}");
    }
}
