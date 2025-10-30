using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;

    [Header("UI References")]
    public HeartUIManager heartUIManager;

    void Awake()
    {
        // Lebenspunkte intern setzen
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Erst jetzt UI updaten, wenn alles initialisiert ist
        if (heartUIManager != null)
        {
            heartUIManager.UpdateHearts(currentHealth);
            Debug.Log($"[PlayerHealth/Start] Player startet mit {currentHealth}/{maxHealth} HP");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] Kein HeartUIManager zugewiesen!");
        }
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return; // Schon tot

        int before = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log($"[PlayerHealth] Schaden: {before} → {currentHealth}");

        heartUIManager?.UpdateHearts(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0) return; // Kein Heal bei Tod

        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerHealth] Heilung: {before} → {currentHealth}");

        if (heartUIManager != null)
        {
            heartUIManager.PlayHeartPickupEffect();
            LeanTween.delayedCall(1.5f, () =>
            {
                heartUIManager.UpdateHearts(currentHealth);
            });
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Spieler gestorben – Objekt deaktiviert.");
        gameObject.SetActive(false);
    }
}
