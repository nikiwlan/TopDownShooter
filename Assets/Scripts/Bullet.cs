using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;      // Bewegungsgeschwindigkeit
    public float lifetime = 2f;    // Sekunden bis zur Selbstzerstörung

    void Start()
    {
        // Kugel zerstört sich nach 'lifetime' Sekunden automatisch
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Bewege dich nach oben relativ zur eigenen Ausrichtung
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }
}
