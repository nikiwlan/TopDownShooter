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

        // 🟢 Kugel auf Bodenebene fixieren (2.5D)
        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;
    }

    void Update()
    {
        // Bewegung in Flugrichtung (vorwärts, also Z)
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Schaden verursachen, falls Gegner vorhanden
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                // Falls der Collider am Child liegt, suche im Parent (z. B. bei Modellen mit mehreren Collidern)
                enemy = other.GetComponentInParent<EnemyBase>();
            }

            if (enemy != null)
            {
                enemy.TakeDamage(1);
            }

            // Kugel zerstören
            Destroy(gameObject);
        }
    }
}
