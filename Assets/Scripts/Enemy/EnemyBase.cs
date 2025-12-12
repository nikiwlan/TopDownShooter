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
    public GameObject bloodHitVFX;

    [Header("Collision")]
    [Tooltip("Sollen alle Enemies von Wänden blockiert werden?")]
    public bool blockWalls = true;

    [Tooltip("Tag, den deine Wände haben (z.B. 'Wall')")]
    public string wallTag = "Wall";

    [Header("Color / Tint (sauber)")]
    [Tooltip("Renderer, die NIE eingefärbt werden sollen (z.B. Laser LineRenderer, VFX, etc.).")]
    public Renderer[] excludeFromTint;

    [Tooltip("Optional: Alle Renderer mit diesem Tag werden NICHT eingefärbt.")]
    public string excludeTintTag = "IgnoreTint";

    [HideInInspector] public PlayerHealth player;

    private Collider[] allColliders;
    private Rigidbody rb;

    // Nur die Renderer, die wirklich getintet werden dürfen
    protected Renderer[] tintRenderers;
    protected Color[] originalColors;

    public Transform Root => transform;

    protected virtual void Awake()
    {
        allColliders = GetComponentsInChildren<Collider>(true);
        rb = GetComponent<Rigidbody>();

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

    // ---------------- COLOR HANDLING (SAUBER) ----------------
    private void BuildTintRendererList()
    {
        var all = GetComponentsInChildren<Renderer>(true);

        // Exclude-Set bauen (schnell & sauber)
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

            // 1) Exclude-Array
            if (excludeSet.Contains(r))
                continue;

            // 2) Optional: Exclude per Tag
            if (!string.IsNullOrEmpty(excludeTintTag) && r.CompareTag(excludeTintTag))
                continue;

            // 3) Nur Renderer, die überhaupt färbbar sind
            //    (LineRenderer hat Material, aber wir wollen ihn meist nicht - deshalb Exclude nutzen)
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                list.Add(r);
        }

        tintRenderers = list.ToArray();

        // Originalfarben speichern
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

    // ---------------- WALL BLOCKING ----------------
    protected virtual void OnTriggerEnter(Collider other)
    {
        HandleWallBlocking(other);
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        HandleWallBlocking(other);
    }

    private void HandleWallBlocking(Collider other)
    {
        if (!blockWalls) return;

        if (!other.CompareTag(wallTag))
            return;

        if (!TryGetComponent<Collider>(out Collider myCol))
            return;

        if (Physics.ComputePenetration(
                myCol, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 direction, out float distance))
        {
            Vector3 separation = direction * distance;
            transform.position += separation;

            if (debug)
                Debug.Log($"[{name}] Hit WALL (Base) → pushed out by {separation.magnitude:0.###}");
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
    }
}
