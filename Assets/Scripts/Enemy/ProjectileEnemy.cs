using UnityEngine;

public class ProjectileEnemy : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float lifetime = 5f;
    public float hitRadius = 0.5f;

    [Header("Audio (optional)")]
    public AudioClip impactSound;

    private Vector3 moveDir;
    private Transform player;

    public void Init(Vector3 dir)
    {
        moveDir = dir.normalized;
    }

    void Start()
    {
        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj) player = pObj.transform;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += moveDir * speed * Time.deltaTime;

        if (!player) return;

        Vector3 projPos = transform.position;
        Vector3 playerPos = player.position;
        projPos.y = 0f;
        playerPos.y = 0f;

        float dist = Vector3.Distance(projPos, playerPos);

        if (dist <= hitRadius)
        {
            if (player.TryGetComponent<PlayerHealth>(out var ph))
                ph.TakeDamage(1, PlayerHealth.DamageType.Range, gameObject);

            if (impactSound)
                AudioManager.Instance.PlaySound3D(impactSound, transform.position);

            Destroy(gameObject);
        }

        if (Physics.Raycast(transform.position, moveDir, out RaycastHit hit, speed * Time.deltaTime))
        {
            if (LayerMask.NameToLayer("Wall") == hit.collider.gameObject.layer)
            {
                if (impactSound)
                    AudioManager.Instance.PlaySound3D(impactSound, transform.position);

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
