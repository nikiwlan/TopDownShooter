using UnityEngine;

public class ProjectileEnemy : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float lifetime = 5f;
    public float hitRadius = 0.5f;

    private Vector3 moveDir;
    private Transform player;

    public void Init(Vector3 dir)
    {
        moveDir = dir.normalized;
    }

    void Start()
    {
        // Player finden (einmalig)
        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj) player = pObj.transform;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Bewege Projektil
        transform.position += moveDir * speed * Time.deltaTime;

        // Prüfe, ob Player existiert
        if (!player) return;

        Vector3 projPos = transform.position;
        Vector3 playerPos = player.position;
        projPos.y = 0f;
        playerPos.y = 0f;

        float dist = Vector3.Distance(projPos, playerPos);

        if (dist <= hitRadius)
        {
            Debug.Log($"[ProjectileEnemy] Treffer! Distanz={dist:F2}");

            if (player.TryGetComponent<PlayerHealth>(out var ph))
                ph.TakeDamage(1);

            Destroy(gameObject);
        }

        // Optional: Treffer an Wände (einfach mit Raycast prüfen)
        if (Physics.Raycast(transform.position, moveDir, out RaycastHit hit, speed * Time.deltaTime))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                Destroy(gameObject);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
#endif
}
