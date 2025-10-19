using UnityEngine;
using UnityEngine.SceneManagement; // Für späteren Game-Over-Reload

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("Player HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
        // Später: SceneLoader.GameOver()
        gameObject.SetActive(false); // vorerst einfach deaktivieren
    }
}

