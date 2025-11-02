using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { Health, FireRate, ScoreBoost, SpeedBoost, TimeSlow }

    [Header("Settings")]
    public PowerUpType type;
    public float duration = 5f;
    public int healthAmount = 1;
    public int scoreBonus = 50;
    public AudioClip pickupSound;
    public GameObject pickupEffect;

    private void Awake()
    {
        // PowerUps sollen immer auf Y=0 liegen
        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var playerHealth = other.GetComponent<PlayerHealth>();
        var playerShooting = other.GetComponent<PlayerShooting>();
        var playerMovement = other.GetComponent<PlayerMovement>();
        var scoreManager = FindObjectOfType<ScoreManager>();
        var enemyControllers = FindObjectsOfType<EnemyController>();
        var uiManager = FindObjectOfType<PowerUpUIManager>();

        Debug.Log($"[PowerUp] {type} eingesammelt von {other.name}");

        switch (type)
        {
            case PowerUpType.Health:
                if (playerHealth != null)
                    playerHealth.Heal(healthAmount);
                break;

            case PowerUpType.FireRate:
                if (playerShooting != null)
                {
                    playerShooting.ApplyFireRateBoost(duration);
                    if (uiManager) uiManager.ShowUI(PowerUpType.FireRate, duration);
                }
                break;

            case PowerUpType.ScoreBoost:
                if (scoreManager != null)
                {
                    scoreManager.ApplyScoreBoost(duration);
                    if (uiManager) uiManager.ShowUI(PowerUpType.ScoreBoost, duration);
                }
                break;

            case PowerUpType.SpeedBoost:
                if (playerMovement != null)
                {
                    playerMovement.ApplySpeedBoost(duration, 1.5f); // 50 % schneller
                    if (uiManager) uiManager.ShowUI(PowerUpType.SpeedBoost, duration);
                }
                break;

            case PowerUpType.TimeSlow:
                foreach (var enemy in enemyControllers)
                    enemy.ApplyTimeSlow(duration, 0.5f);
                if (uiManager) uiManager.ShowUI(PowerUpType.TimeSlow, duration);
                break;
        }

        // ✨ Effekte & Sound
        if (pickupEffect)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        if (pickupSound)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }
}
