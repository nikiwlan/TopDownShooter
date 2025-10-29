using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Settings")]
    public int health = 1;
    public int pointsOnKill = 10;

    private void OnTriggerEnter(Collider collision) // 🔄 2D → 3D
    {
        Debug.Log("[EnemyHealth] Enemy triggered with: " + collision.name);

        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }

            Die(); // 👈 Gegner verschwindet nach Kollision mit Spieler
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
