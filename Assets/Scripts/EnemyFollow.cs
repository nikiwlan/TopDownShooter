using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyFollow : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;

    private Transform player;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 🟢 Sicherstellen, dass der Gegner auf Bodenhöhe bleibt
        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Bewegung in XZ-Ebene (Y bleibt konstant)
        Vector3 direction = (player.position - transform.position);
        direction.y = 0f; // kein Höhenunterschied
        direction.Normalize();

        Vector3 newPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    void OnCollisionEnter(Collision collision) // 🔄 2D → 3D
    {
        Debug.Log("[EnemyFollow] Enemy collided with: " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("[EnemyFollow] Enemy hit Player!");
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(1);

            Destroy(gameObject);
        }
    }
}
