using UnityEngine;

public class ProjectileEnemy : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float lifetime = 5f;

    private Vector3 moveDirection;

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
        // Fliege konstant geradeaus
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
            // Falls es was anderes trifft (Wand etc.), ebenfalls zerstören
            Destroy(gameObject);
        }
    }
}
