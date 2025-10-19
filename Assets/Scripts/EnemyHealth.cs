using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Enemy triggered with: " + collision.name);
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }

            // Gegner zerstören
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
