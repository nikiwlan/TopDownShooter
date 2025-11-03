using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 40f;
    public float lifetime = 5f;
    public int damage = 1;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 🟢 Keine Gravitation für Top-Down-Bullets
        rb.useGravity = false;

        // 🟢 Vorwärtsbewegung mit Physik
        rb.velocity = transform.forward * speed;

        // 🟢 Sicheres Auto-Despawn
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Nur reagieren, wenn Gegner getroffen
        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy == null)
                enemy = other.GetComponentInParent<EnemyBase>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"Enemy getroffen! Schaden: {damage}");
            }

            Destroy(gameObject); // Kugel entfernen
        }
    }
}
