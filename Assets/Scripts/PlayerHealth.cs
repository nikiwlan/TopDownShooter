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
        currentHealth = maxHealth;
        Debug.Log($"[PlayerHealth/Awake] maxHealth={maxHealth}, currentHealth={currentHealth}, UI set? {heartUIManager != null}");

        if (heartUIManager != null)
            heartUIManager.UpdateHearts(currentHealth);
    }

    void Start()
    {
        Debug.Log("[PlayerHealth/Start] calling UpdateHearts again for safety.");
        if (heartUIManager != null)
            heartUIManager.UpdateHearts(currentHealth);
        else
            Debug.LogWarning("[PlayerHealth/Start] heartUIManager is NULL. Bitte im Inspector PlayerHealth -> Heart UI Manager zuweisen (UI-Objekt).");
    }

    public void TakeDamage(int amount)
    {
        int before = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log($"[PlayerHealth/TakeDamage] {before} -> {currentHealth}");

        if (heartUIManager != null) heartUIManager.UpdateHearts(currentHealth);
        if (currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        if (heartUIManager != null)
        {
            // Zuerst nur die Animation abspielen (Herz fliegt)
            heartUIManager.PlayHeartPickupEffect();

            // Nach 1 Sekunde die UI aktualisieren (Herz oben sichtbar machen)
            LeanTween.delayedCall(1.5f, () =>
            {
                heartUIManager.UpdateHearts(currentHealth);
            });
        }

        Debug.Log($"[Heal] +{currentHealth - before} HP → {currentHealth}");
    }


    void Die()
    {
        Debug.Log("[PlayerHealth/Die] Player disabled.");
        gameObject.SetActive(false);
    }
}
