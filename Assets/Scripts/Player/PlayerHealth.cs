using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public enum DamageType
    {
        Range,
        Melee
    }

    [Header("Health Settings")]
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;

    [Header("Shield Settings")]
    public int maxShieldCharges = 2;
    [SerializeField] private int shieldCharges = 0;

    [Header("Shield Sound")]
    public AudioClip shieldHitSound;   // Sound wenn Shield 1 Charge verliert

    [Header("Shield VFX")]
    [SerializeField] private GameObject shieldVfx;

    public bool HasShield => shieldCharges > 0;
    public int ShieldCharges => shieldCharges;

    [Header("Invincibility Settings")]
    public float invincibilityDuration = 0.7f;
    private bool isInvincible = false;

    [Header("UI References")]
    public HeartUIManager heartUIManager;
    public DamageFlash damageFlash;

    [Header("Animation")]
    public Animator animator; 

    // ---------------------- AUDIO ----------------------
    [Header("Damage Sounds (Randomized)")]
    public AudioClip damageSound1;
    public AudioClip damageSound2;
    public AudioClip damageSound3;

    [Header("Death Sounds (Played In Order)")]
    public AudioClip deathSound_1;      // Stöhnen
    public AudioClip deathSound_2;      // Body fall

    // ---------------------- VFX ----------------------
    [Header("Blood VFX")]
    public GameObject rangeBloodVFX;
    public GameObject meleeBloodVFX;

    [HideInInspector]
    public DamageType lastDamageType = DamageType.Range;

    // ✅ Schild soll NUR Enemy-Projectiles blocken
    [Header("Shield Filters")]
    [Tooltip("Nur wenn der Schaden von diesem Tag kommt, blockt das Schild Range-Damage.")]
    public string shieldBlocksOnlyTag = "EnemyProjectile";

    [Tooltip("Optional: wenn du lieber über Layer blocken willst (z.B. EnemyProjectile Layer), trage ihn hier ein. -1 = deaktiviert")]
    public int shieldBlocksOnlyLayer = -1;

    [Header("Camera Reference")]
    private CameraFollow cameraScript;

    private Renderer[] bodyParts;

    private bool isDead = false; // Verhindert Doppeltod

    private void UpdateShieldVfx()
    {
        if (shieldVfx != null)
            shieldVfx.SetActive(shieldCharges > 0);
    }

    void Awake()
    {
        currentHealth = maxHealth;
        bodyParts = GetComponentsInChildren<Renderer>();
    }

    void Start()
    {
        if (heartUIManager != null)
        {
            heartUIManager.UpdateHearts(currentHealth);
            heartUIManager.UpdateShield(shieldCharges);
            UpdateShieldVfx();
            Debug.Log($"[PlayerHealth] Player startet mit {currentHealth}/{maxHealth} HP");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] Kein HeartUIManager zugewiesen!");
        }
        if (Camera.main != null)
        {
            cameraScript = Camera.main.GetComponent<CameraFollow>();
        }
    }

    // ------------------------------------------------------------
    // DAMAGE
    // ------------------------------------------------------------

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, DamageType.Melee, null, -1);
    }

    public void TakeDamage(int amount, DamageType type, GameObject source = null)
    {
        string srcTag = source != null ? source.tag : null;
        int srcLayer = source != null ? source.layer : -1;
        TakeDamage(amount, type, srcTag, srcLayer);
    }

    public void TakeDamage(int amount, DamageType type, string sourceTag, int sourceLayer)
    {
        // Wenn tot oder unverwundbar -> Abbruch
        if (isInvincible || isDead) return;

        lastDamageType = type;

        // Schild-Logik
        if (type == DamageType.Range && shieldCharges > 0)
        {
            bool tagOk = !string.IsNullOrEmpty(shieldBlocksOnlyTag) && sourceTag == shieldBlocksOnlyTag;
            bool layerOk = (shieldBlocksOnlyLayer >= 0) && (sourceLayer == shieldBlocksOnlyLayer);

            if (tagOk || layerOk)
            {
                shieldCharges = Mathf.Max(0, shieldCharges - 1);

                if (shieldHitSound != null)
                    AudioManager.Instance.PlaySound2D(shieldHitSound);

                heartUIManager?.UpdateShield(shieldCharges);
                UpdateShieldVfx();
                return;
            }
        }

        if (cameraScript != null)
        {
            cameraScript.TriggerDamageShake();
        }

        StartCoroutine(BecomeTemporarilyInvincible());

        if (damageFlash != null)
            damageFlash.Flash();

        SpawnBloodVFX();

        int before = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        Debug.Log($"[PlayerHealth] Schaden: {before} → {currentHealth}");

        heartUIManager?.UpdateHearts(currentHealth);
        PlayRandomDamageSound();

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator BecomeTemporarilyInvincible()
    {
        isInvincible = true;
        float blinkInterval = 0.1f;
        float timer = 0f;

        while (timer < invincibilityDuration)
        {
            // --- HIER IST DIE ÄNDERUNG ---
            if (bodyParts != null)
            {
                // Gehe durch JEDES gefundene Teil (Kopf, Arme, Beine...)
                foreach (var part in bodyParts)
                {
                    // WICHTIG: Das Schild-VFX soll NICHT mitblinken, falls es an ist!
                    if (part.gameObject != shieldVfx)
                        part.enabled = !part.enabled; // An/Aus umschalten
                }
            }
            // -----------------------------

            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        // AM ENDE: Alles wieder sichtbar machen
        if (bodyParts != null)
        {
            foreach (var part in bodyParts)
                part.enabled = true;
        }

        isInvincible = false;
    }

    private void SpawnBloodVFX()
    {
        GameObject prefab = null;

        switch (lastDamageType)
        {
            case DamageType.Melee:
                prefab = meleeBloodVFX;
                break;
            case DamageType.Range:
            default:
                prefab = rangeBloodVFX;
                break;
        }

        if (prefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 1f;
        GameObject vfx = Instantiate(prefab, spawnPos, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
        Destroy(vfx, 1f);
    }

    private void PlayRandomDamageSound()
    {
        AudioClip[] clips = new AudioClip[] { damageSound1, damageSound2, damageSound3 };
        var valid = new System.Collections.Generic.List<AudioClip>();
        foreach (var c in clips) if (c != null) valid.Add(c);

        if (valid.Count == 0) return;
        int index = Random.Range(0, valid.Count);
        AudioManager.Instance.PlaySound2D(valid[index]);
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;

        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        if (heartUIManager != null)
        {
            heartUIManager.PlayHeartPickupEffect();
            LeanTween.delayedCall(1.5f, () =>
            {
                heartUIManager.UpdateHearts(currentHealth);
            });
        }
    }

    public void GiveShield(int charges)
    {
        shieldCharges = Mathf.Clamp(charges, 0, maxShieldCharges);
        heartUIManager?.UpdateShield(shieldCharges);
        UpdateShieldVfx();
    }

    // ---------------------- NEUE TODES LOGIK ----------------------
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. Animator Trigger setzen
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // 2. Bewegung ausschalten (Referenz holen und deaktivieren)
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        // (Optional: Auch Schießen deaktivieren)
        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null) shooting.enabled = false;

        // 3. Audio & Logik starten
        StartCoroutine(PlayDeathSequence());
    }

    private IEnumerator PlayDeathSequence()
    {
        // Erster Sound (Stöhnen)
        if (deathSound_1 != null)
            AudioManager.Instance.PlaySound2D(deathSound_1);

        // Warte kurz auf den Aufprall in der Animation
        yield return new WaitForSeconds(0.6f);

        // Zweiter Sound (Körper fällt)
        if (deathSound_2 != null)
            AudioManager.Instance.PlaySound2D(deathSound_2);

        // HIER WICHTIG: Kein SetActive(false)! Wir lassen die Leiche liegen.
    }
}