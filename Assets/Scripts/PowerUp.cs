using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { Health, FireRate, ScoreBoost }

    [Header("Settings")]
    public PowerUpType type;
    public float duration = 5f;     // Für temporäre PowerUps
    public int healthAmount = 1;    // Health Pack
    public int scoreBonus = 50;     // Optional für Sofortpunkte

    public AudioClip pickupSound;
    public GameObject pickupEffect;

    [Header("UI")]
    [SerializeField] private PowerUpUI ui;   // Im Inspector zuweisen (oder auto-find)

    void Awake()
    {
        if (ui == null) ui = FindObjectOfType<PowerUpUI>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var playerHealth = other.GetComponent<PlayerHealth>();
        var playerShooting = other.GetComponent<PlayerShooting>();
        var scoreManager = FindObjectOfType<ScoreManager>();

        Debug.Log($"[PowerUp] {type} eingesammelt von {other.name}");

        switch (type)
        {
            // ❤️ HEALTH POWER-UP
            case PowerUpType.Health:
                if (playerHealth != null)
                {
                    Debug.Log("[PowerUp] Health-Pickup erkannt!");
                    playerHealth.Heal(healthAmount);
                }
                else
                {
                    Debug.LogWarning("[PowerUp] Kein PlayerHealth gefunden!");
                }
                break;

            // 🔫 FIRE RATE BOOST
            case PowerUpType.FireRate:
                if (playerShooting != null)
                {
                    Debug.Log("[PowerUp] FireRate Boost aktiviert!");
                    playerShooting.ApplyFireRateBoost(duration);
                    if (ui) ui.ShowPowerUp(PowerUpType.FireRate, "FIRE RATE BOOST", duration);
                }
                else
                {
                    Debug.LogWarning("[PowerUp] Kein PlayerShooting gefunden!");
                }
                break;

            // 💰 SCORE BOOST
            case PowerUpType.ScoreBoost:
                if (scoreManager != null)
                {
                    Debug.Log("[PowerUp] ScoreBoost aktiviert!");
                    scoreManager.ApplyScoreBoost(duration);
                    if (ui) ui.ShowPowerUp(PowerUpType.ScoreBoost, "SCORE BOOST", duration);
                }
                else
                {
                    Debug.LogWarning("[PowerUp] Kein ScoreManager gefunden!");
                }
                break;
        }

        // ✨ Effekte & Sound
        if (pickupEffect)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        if (pickupSound)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Debug.Log($"[PowerUp] {type} erfolgreich angewendet – Objekt wird zerstört.");
        Destroy(gameObject);
    }
}
