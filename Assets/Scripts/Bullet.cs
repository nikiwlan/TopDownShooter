using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 2f;

    void Start()
    {
        // Bullet zerstört sich nach X Sekunden
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Bewege das Bullet nach oben (lokale Richtung)
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Schaden verursachen, falls EnemyHealth vorhanden
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
                enemy.TakeDamage(1);

            // Kugel zerstören
            Destroy(gameObject);
        }
    }

}
