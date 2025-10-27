using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { Health, FireRate, ScoreBoost }

    [Header("Settings")]
    public PowerUpType type;
    public float duration = 5f;     // für temporäre PowerUps
    public int healthAmount = 1;    // Health Pack
    public int scoreBonus = 50;     // optional für Sofortpunkte

    public AudioClip pickupSound;
    public GameObject pickupEffect;

    [Header("UI")]
    [SerializeField] private PowerUpUI ui;   // im Inspector zuweisen (oder auto-find)

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

        Debug.Log($"[PowerUp] {type} eingesammelt!");

        switch (type)
        {
            case PowerUpType.Health:
                if (playerHealth != null)
                {
                    int before = playerHealth.currentHealth;
                    playerHealth.currentHealth = Mathf.Min(playerHealth.maxHealth, before + healthAmount);
                    Debug.Log($"[PowerUp] +{playerHealth.currentHealth - before} HP → {playerHealth.currentHealth}");
                }
                break;

            case PowerUpType.FireRate:
                if (playerShooting != null)
                {
                    Debug.Log("[PowerUp] FireRate Boost aktiviert!");
                    playerShooting.ApplyFireRateBoost(duration);
                    if (ui) ui.ShowPowerUp("\u26A1 Fire Rate", duration);
                }
                break;

            case PowerUpType.ScoreBoost:
                if (scoreManager != null)
                {
                    Debug.Log("[PowerUp] ScoreBoost aktiviert!");
                    scoreManager.ApplyScoreBoost(duration);
                    if (ui) ui.ShowPowerUp("Score Boost", duration);
                }
                break;
        }

        if (pickupEffect) Instantiate(pickupEffect, transform.position, Quaternion.identity);
        if (pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }
}
