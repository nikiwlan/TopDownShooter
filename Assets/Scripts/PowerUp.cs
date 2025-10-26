using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        Health,
        FireRate,
        ScoreBoost
    }

    [Header("Settings")]
    public PowerUpType type;
    public float duration = 5f; // Nur für temporäre PowerUps
    public int healthAmount = 1; // Für Health Pack
    public int scoreBonus = 50; // Für Score Boost

    public AudioClip pickupSound;        // Soundeffekt beim Einsammeln
    public GameObject pickupEffect;      // Partikeleffekt beim Einsammeln


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        PlayerShooting playerShooting = other.GetComponent<PlayerShooting>();
        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();

        Debug.Log($"[PowerUp] {type} eingesammelt!");

        switch (type)
        {
            case PowerUpType.Health:
                if (playerHealth != null)
                {
                    int oldHealth = playerHealth.currentHealth;
                    playerHealth.currentHealth = Mathf.Min(
                        playerHealth.maxHealth,
                        playerHealth.currentHealth + healthAmount
                    );

                    Debug.Log($"[PowerUp] +{playerHealth.currentHealth - oldHealth} HP! Aktuelle HP: {playerHealth.currentHealth}");
                }
                break;

            case PowerUpType.FireRate:
                if (playerShooting != null)
                {
                    Debug.Log("[PowerUp] FireRate Boost aktiviert!");
                    playerShooting.StartCoroutine(playerShooting.TempFireRateBoost(duration));
                }
                break;

            case PowerUpType.ScoreBoost:
                if (scoreManager != null)
                {
                    Debug.Log("[PowerUp] ScoreBoost aktiviert!");
                    scoreManager.StartCoroutine(scoreManager.TempScoreBoost(duration, scoreBonus));
                }
                break;
        }

        // Optional: visueller oder akustischer Effekt beim Aufsammeln
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject); // PowerUp verschwindet nach Einsammeln
    }
}
