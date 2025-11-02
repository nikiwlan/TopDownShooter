using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ProjectileEnemy : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float lifetime = 5f;

    private Vector3 moveDirection;

    void Awake()
    {
        // SphereCollider konfigurieren
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
    }

    // Wird direkt nach dem Erzeugen aufgerufen, um Flugrichtung zu setzen
    public void Init(Vector3 dir)
    {
        moveDirection = dir.normalized;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Fliege konstant geradeaus (ohne Physik)
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[ProjectileEnemy] Hit Player!");

            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null)
                player.TakeDamage(1);

            Destroy(gameObject);
        }
        else if (!other.CompareTag("Enemy"))
        {
            // Wenn Projektil etwas anderes trifft (z. B. Wand), zerstören
            Destroy(gameObject);
        }
    }
}
