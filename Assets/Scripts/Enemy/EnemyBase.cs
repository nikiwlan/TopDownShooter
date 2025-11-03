using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Lebenspunkte des Gegners.")]
    public int health = 3;

    [Tooltip("Punkte, die dieser Gegnertyp beim Tod gibt.")]
    public int pointsOnKill = 10;

    [Header("Debug")]
    public bool debug = true;

    [HideInInspector] public PlayerHealth player;

    // ----------------------------------------------------
    // INITIALISIERUNG
    // ----------------------------------------------------
    protected virtual void Awake()
    {
        var col = GetComponent<Collider>();
        if (col == null)
            Debug.LogError($"[{name}] ❌ Kein Collider gefunden!");
        else
            Debug.Log($"[{name}] ✅ Collider erkannt. isTrigger={col.isTrigger}, layer={LayerMask.LayerToName(gameObject.layer)}");

        var rb = GetComponent<Rigidbody>();
        Debug.Log($"[{name}] Rigidbody vorhanden: {rb != null}");
    }

    protected virtual void Start()
    {
        var pObj = GameObject.FindGameObjectWithTag("Player");
        player = pObj ? pObj.GetComponent<PlayerHealth>() : null;

        if (debug)
            Debug.Log($"[{name}] Player reference: {(player ? "FOUND" : "MISSING")}");
    }

    // ----------------------------------------------------
    // TIME-SLOW INTEGRATION (wirkt auch für später gespawnte Gegner)
    // ----------------------------------------------------
    protected virtual void OnEnable()
    {
        // Wenn ein globaler TimeSlow aktiv ist und dieser Gegner verlangsambar ist,
        // dann direkt anwenden (restliche Dauer).
        if (GameEffectsManager.TimeSlowActive && this is ITimeSlowable slowable)
        {
            slowable.ApplyTimeSlow(GameEffectsManager.Remaining, GameEffectsManager.Factor);
        }
    }

    // ----------------------------------------------------
    // SCHADEN & TOD
    // ----------------------------------------------------
    public virtual void TakeDamage(int amount)
    {
        health -= amount;
        if (debug)
            Debug.Log($"[{name}] Nimmt {amount} Schaden → verbleibend: {health}");

        if (health <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Debug.Log($"[EnemyBase] 💀 {gameObject.name} gestorben → +{pointsOnKill} Punkte");

        // Punkte hinzufügen, wenn ScoreManager vorhanden ist
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(pointsOnKill);
        }
        else
        {
            Debug.LogWarning("[EnemyBase] ⚠️ Kein ScoreManager gefunden!");
        }

        Destroy(gameObject);
    }

    // ----------------------------------------------------
    // TRIGGER / DEBUG
    // ----------------------------------------------------
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!debug) return;

        Debug.Log($"[{name}] BASE OnTriggerEnter → {other.name} (Tag={other.tag}, Layer={LayerMask.LayerToName(other.gameObject.layer)})");
    }
}
