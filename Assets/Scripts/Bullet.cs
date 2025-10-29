using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 20f;
    public float lifetime = 2f;

    void Start()
    {
        // Zerstöre die Kugel nach einer bestimmten Zeit
        Destroy(gameObject, lifetime);

        // 🟢 Sicherstellen, dass Kugel auf Bodenebene bleibt
        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;
    }

    void Update()
    {
        // Bewegung in Flugrichtung (vorwärts, also Z)
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
    }

    void OnTriggerEnter(Collider other) // 🔄 2D → 3D
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
