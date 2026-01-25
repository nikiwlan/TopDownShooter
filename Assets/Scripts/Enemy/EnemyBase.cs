using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int health = 3;
    public int pointsOnKill = 10;
    public int spawnPowerUpCredits = 1;

    [Header("Debug")]
    public bool debug = true;

    [Header("VFX")]
    public GameObject bloodHitVFX;

    [Header("Collision")]
    public bool blockWalls = true;
    public string wallTag = "Wall";

    [Header("Wall -> Forced Run (away from spawn)")]
    [Tooltip("Wenn true: Nach Wall-Kontakt läuft der Enemy für X Meter WEG vom Spawnpunkt.")]
    public bool runAwayFromSpawnOnWallHit = true;

    [Tooltip("Wie weit soll er nach Wall-Hit weglaufen? (Meter)")]
    public float forcedRunDistance = 2.5f;

    [Tooltip("Cooldown, damit er nicht 100x pro Sekunde neu startet (Jitter).")]
    public float forcedRunCooldown = 0.25f;

    [Tooltip("Kleiner Extra-Abstand beim Rausdrücken, um 'kleben' zu vermeiden.")]
    public float wallSkin = 0.02f;

    [Header("Color / Tint (sauber)")]
    public Renderer[] excludeFromTint;
    public string excludeTintTag = "IgnoreTint";

    [HideInInspector] public PlayerHealth player;

    private Collider[] allColliders;
    private Rigidbody rb;

    protected Renderer[] tintRenderers;
    protected Color[] originalColors;

    public Transform Root => transform;

    // ---- Override API ----
    public bool HasForcedMove => forcedRemaining > 0f;
    public Vector3 ForcedMoveDirection => forcedDir;
    public float ForcedMoveRemaining => forcedRemaining;

    private Vector3 forcedDir;
    private float forcedRemaining;
    private float nextForcedAllowedTime;

    // Spawnpunkt merken
    private Vector3 spawnPos;

    protected virtual void Awake()
    {
        allColliders = GetComponentsInChildren<Collider>(true);
        rb = GetComponent<Rigidbody>();

        spawnPos = transform.position; // <-- Spawn merken

        BuildTintRendererList();

        if (debug)
        {
            Debug.Log($"[{name}] Tint Renderer Count: {tintRenderers.Length}");
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

    public void ConsumeForcedMove(float movedDistance)
    {
        if (forcedRemaining <= 0f) return;

        forcedRemaining -= Mathf.Max(0f, movedDistance);
        if (forcedRemaining < 0f) forcedRemaining = 0f;
    }

    private void StartForcedRunAwayFromSpawn()
    {
        if (!runAwayFromSpawnOnWallHit) return;
        if (Time.time < nextForcedAllowedTime) return;

        Vector3 dir = (transform.position - spawnPos);
        dir.y = 0f;

        // Falls spawnPos == currentPos (sehr selten), nimm fallback Richtung "raus"
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        forcedDir = dir.normalized;
        forcedRemaining = forcedRunDistance;
        nextForcedAllowedTime = Time.time + forcedRunCooldown;

        if (debug)
            Debug.Log($"[{name}] ForcedRunAwayFromSpawn START → {forcedRunDistance:0.##}m");
    }

    // ---------------- COLOR HANDLING ----------------
    private void BuildTintRendererList()
    {
        var all = GetComponentsInChildren<Renderer>(true);

        var excludeSet = new System.Collections.Generic.HashSet<Renderer>();
        if (excludeFromTint != null)
        {
            foreach (var r in excludeFromTint)
                if (r != null) excludeSet.Add(r);
        }

        var list = new System.Collections.Generic.List<Renderer>(all.Length);

        foreach (var r in all)
        {
            if (r == null) continue;
            if (excludeSet.Contains(r)) continue;
            if (!string.IsNullOrEmpty(excludeTintTag) && r.CompareTag(excludeTintTag)) continue;

            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                list.Add(r);
        }

        tintRenderers = list.ToArray();

        originalColors = new Color[tintRenderers.Length];
        for (int i = 0; i < tintRenderers.Length; i++)
        {
            var mat = tintRenderers[i].material;
            if (mat != null && mat.HasProperty("_Color"))
                originalColors[i] = mat.color;
        }
    }

    public void SetColorAll(Color c)
    {
        for (int i = 0; i < tintRenderers.Length; i++)
        {
            var r = tintRenderers[i];
            if (r == null) continue;

            var mat = r.material;
            if (mat != null && mat.HasProperty("_Color"))
                mat.color = c;
        }
    }

    public void ResetColorAll()
    {
        for (int i = 0; i < tintRenderers.Length; i++)
        {
            var r = tintRenderers[i];
            if (r == null) continue;

            var mat = r.material;
            if (mat != null && mat.HasProperty("_Color"))
                mat.color = originalColors[i];
        }
    }

    // ---------------- DAMAGE / DEATH ----------------
    public virtual void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector3.forward, transform.position);
    }

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

    protected void SpawnBloodVFX(Vector3 hitDir, Vector3 hitPoint)
    {
        if (bloodHitVFX == null)
            return;

        Vector3 dir = hitDir.normalized;

        float depthSize = 0.5f;
        if (TryGetComponent<Collider>(out Collider col))
            depthSize = col.bounds.extents.z;

        float offsetDistance = depthSize * 0.4f;

        Vector3 spawnPosVfx =
            transform.position
            + Vector3.up * 0.9f
            + dir * offsetDistance;

        float randomRot = Random.Range(0f, 360f);

        GameObject vfx = Instantiate(
            bloodHitVFX,
            spawnPosVfx,
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

        // ---------------------------------------------------------
        // 2. NEU: Credits an den PowerUpSpawner senden
        // ---------------------------------------------------------
        if (PowerUpSpawner.Instance != null)
        {
            PowerUpSpawner.Instance.AddCredits(spawnPowerUpCredits);
        }
        // ---------------------------------------------------------

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

    // ---------------- WALL BLOCKING ----------------
    protected virtual void OnTriggerEnter(Collider other) => HandleWallBlocking(other);
    protected virtual void OnTriggerStay(Collider other) => HandleWallBlocking(other);
    protected virtual void OnTriggerExit(Collider other) { }

    private void HandleWallBlocking(Collider other)
    {
        if (!blockWalls) return;
        if (!other.CompareTag(wallTag)) return;

        if (!TryGetComponent<Collider>(out Collider myCol))
            return;

        if (Physics.ComputePenetration(
                myCol, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 direction, out float distance))
        {
            // Push out
            Vector3 separation = direction * (distance + wallSkin);
            transform.position += separation;

            // Forced run away from spawn
            StartForcedRunAwayFromSpawn();

            if (debug)
                Debug.Log($"[{name}] Hit WALL → push {separation.magnitude:0.###} + forced-away-from-spawn");
        }
    }
}
