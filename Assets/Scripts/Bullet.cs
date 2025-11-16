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
                enemy.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        // 2️⃣ Gate getroffen → Sound sicher abspielen
        if (other.CompareTag("Gate"))
        {
            if (gateHitSound != null)
            {
                GameObject audioObj = new GameObject("GateHitSound");
                AudioSource src = audioObj.AddComponent<AudioSource>();
                src.clip = gateHitSound;
                src.volume = gateSoundVolume;
                src.spatialBlend = 0f;
                src.Play();
                Destroy(audioObj, gateHitSound.length);
            }

            Destroy(gameObject);
            return;
        }

        // 3️⃣ Wand getroffen
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }
    }
}
