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
}
