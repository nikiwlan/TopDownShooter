using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 1; // einfache HP für jetzt

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}

