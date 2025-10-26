using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 1;
    public int pointsOnKill = 10;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("[EnemyHealth] Enemy triggered with: " + collision.name);

        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }

            Die(); // 👈 Statt direkt zerstören
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"[EnemyHealth] Took {damage} damage, remaining HP: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("[EnemyHealth] Enemy died.");

        // Punkte vergeben, falls ScoreManager existiert
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(pointsOnKill);
            Debug.Log($"[EnemyHealth] +{pointsOnKill} Punkte vergeben!");
        }
        else
        {
            Debug.LogWarning("[EnemyHealth] ScoreManager.Instance ist NULL!");
        }

        Destroy(gameObject);
    }
}
