using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
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
    private Vector3 dir;
    private float dieAt;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        // Wichtig: Collider muss Trigger sein
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    // Wird von PlayerShooting gesetzt
    public void Init(Vector3 direction, float spd, float life)
    {
        dir = direction;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.000001f) dir = transform.forward;
        dir.Normalize();

        speed = spd;
        lifetime = life;
        dieAt = Time.time + lifetime;

        transform.rotation = Quaternion.LookRotation(dir);
    }

    void FixedUpdate()
    {
        if (Time.time >= dieAt)
        {
            Destroy(gameObject);
            return;
        }

        rb.MovePosition(rb.position + dir * (speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug um zu sehen WAS getroffen wird
        Debug.Log("BULLET hit: " + other.tag);

        if (other.CompareTag("EnemyHead"))
        {
            BossBeetle boss = other.GetComponentInParent<BossBeetle>();
            if (boss != null)
                boss.OnHeadHit(damage, dir, transform.position);

            Destroy(gameObject);
            return;
        }


        if (other.CompareTag("EnemyBody"))
        {
            BossBeetle boss = other.GetComponentInParent<BossBeetle>();
            if (boss != null)
                boss.OnBodyHit(transform.position);

            Destroy(gameObject);
            return;
        }


        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null)
                enemy.TakeDamage(damage, dir, transform.position);

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Gate"))
        {
            if (gateHitSound != null)
                AudioManager.Instance.PlaySound2D(gateHitSound, gateSoundVolume);

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }
    }
}
