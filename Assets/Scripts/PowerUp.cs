using System.Linq;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        Health,
        FireRate,
        ScoreBoost,
        SpeedBoost,
        TimeSlow
    }

    [Header("General Settings")]
    public PowerUpType type;
    public float duration = 5f;
    public AudioClip pickupSound;
    public GameObject pickupEffect;

    [Header("Specific Settings")]
    public int healthAmount = 1;
    public int scoreBonus = 50;

    [Header("Visual Animation Settings")]
    public bool rotate = true;
    public float rotationSpeed = 40f;
    public bool floatUpDown = true;
    public float floatAmplitude = 0.25f;
    public float floatFrequency = 2f;
    [Tooltip("Wie stark das PowerUp nach vorne geneigt ist (sichtbarer in TopDown)")]
    public float tiltAngle = 90f;

    private Vector3 startPos;
    private Transform visualChild;

    // -------------------- 🔹 NEU: Flag für Spawn-Rotation --------------------
    private bool initializedTilt = false;
    // -----------------------------------------------------------------------

    private void Awake()
    {
        // auf Bodenhöhe setzen
        Vector3 pos = transform.position;
        pos.y = 1.5f;
        transform.position = pos;
    }

    private void Start()
    {
        // 🔹 visuelles Kind finden
        if (transform.childCount > 0)
            visualChild = transform.GetChild(0);

        if (visualChild != null)
        {
            startPos = visualChild.localPosition;

            // 🔹 Drehung für sichtbare Neigung (X-Achse!)
            visualChild.localRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
            initializedTilt = true;
        }
    }

    private void Update()
    {
        if (visualChild == null)
            return;

        // 🔹 Nur einmal sicherstellen, dass die Neigung bleibt (z. B. nach Spawner-Reset)
        if (!initializedTilt)
        {
            visualChild.localRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
            initializedTilt = true;
        }

        // 🔹 Rotation um Y-Achse
        if (rotate)
            visualChild.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // 🔹 Schweben (Sinusbewegung)
        if (floatUpDown)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            visualChild.localPosition = new Vector3(startPos.x, newY, startPos.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var playerHealth = other.GetComponent<PlayerHealth>();
        var playerShooting = other.GetComponent<PlayerShooting>();
        var playerMovement = other.GetComponent<PlayerMovement>();
        var scoreManager = FindObjectOfType<ScoreManager>();
        var uiManager = FindObjectOfType<PowerUpUIManager>();

        Debug.Log($"[PowerUp] {type} eingesammelt von {other.name}");

        switch (type)
        {
            case PowerUpType.Health:
                if (playerHealth) playerHealth.Heal(healthAmount);
                break;

            case PowerUpType.FireRate:
                if (playerShooting)
                {
                    playerShooting.ApplyFireRateBoost(duration);
                    uiManager?.ShowUI(PowerUpType.FireRate, duration);
                }
                break;

            case PowerUpType.ScoreBoost:
                if (scoreManager)
                {
                    scoreManager.ApplyScoreBoost(duration);
                    uiManager?.ShowUI(PowerUpType.ScoreBoost, duration);
                }
                break;

            case PowerUpType.SpeedBoost:
                if (playerMovement)
                {
                    playerMovement.ApplySpeedBoost(duration, 1.5f);
                    uiManager?.ShowUI(PowerUpType.SpeedBoost, duration);
                }
                break;

            case PowerUpType.TimeSlow:
                GameEffectsManager.ActivateTimeSlow(duration, 0.5f);
                var slowables = FindObjectsOfType<MonoBehaviour>().OfType<ITimeSlowable>();
                foreach (var s in slowables)
                    s.ApplyTimeSlow(duration, 0.5f);
                uiManager?.ShowUI(PowerUpType.TimeSlow, duration);
                break;
        }

        if (pickupEffect)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        if (pickupSound)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Destroy(gameObject);
    }
}
