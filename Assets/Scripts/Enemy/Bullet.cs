using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 40f;
    public float lifetime = 5f;
    public int damage = 1;

    [Header("Audio")]
    public AudioClip gateHitSound;
    public float gateSoundVolume = 1f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.velocity = transform.forward * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1️⃣ Enemy getroffen
        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy == null)
                enemy = other.GetComponentInParent<EnemyBase>();

            if (enemy != null)
            {
                enemy.TakeDamage(
                    damage,
                    transform.forward,        // Richtung
                    transform.position        // Einschlagspunkt
                );
            }

            Destroy(gameObject);
            return;
        }

        // 2️⃣ Gate getroffen → Sound über AudioManager
        if (other.CompareTag("Gate"))
        {
            if (gateHitSound != null)
            {
                // ⭐ 2D-Sound, damit Treffer immer hörbar bleibt
                AudioManager.Instance.PlaySound2D(gateHitSound, gateSoundVolume);
            }

            Destroy(gameObject);
            return;
        }

        // 3️⃣ Wand getroffen → einfach verschwinden
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }
    }
}