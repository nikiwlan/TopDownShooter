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

    public bool HasShield => shieldCharges > 0;
    public int ShieldCharges => shieldCharges;

    [Header("Invincibility Settings")]
    public float invincibilityDuration = 0.5f;
    private bool isInvincible = false;

    [Header("UI References")]
    public HeartUIManager heartUIManager;
    public DamageFlash damageFlash;

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

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        if (heartUIManager != null)
        {
            heartUIManager.UpdateHearts(currentHealth);

            heartUIManager.UpdateShield(shieldCharges);

            Debug.Log($"[PlayerHealth] Player startet mit {currentHealth}/{maxHealth} HP");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] Kein HeartUIManager zugewiesen!");
        }
    }

    // ------------------------------------------------------------
    // DAMAGE
    // ------------------------------------------------------------

    // Keep old signature: falls irgendwo noch player.TakeDamage(1) steht
    // Default ist MELEE (damit Shield nicht random alles blockt)
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, DamageType.Melee, null, -1);
    }

    // klare Damage-API
    public void TakeDamage(int amount, DamageType type, GameObject source = null)
    {
        string srcTag = source != null ? source.tag : null;
        int srcLayer = source != null ? source.layer : -1;
        TakeDamage(amount, type, srcTag, srcLayer);
    }

    // wenn du direkt Tag/Layer übergeben willst
    public void TakeDamage(int amount, DamageType type, string sourceTag, int sourceLayer)
    {
        if (isInvincible)
        {
            Debug.Log("[PlayerHealth] Schaden geblockt dank Grace Period.");
            return;
        }

        lastDamageType = type;

        // Schild blockt NUR:
        // - Range-Schaden
        // - es gibt Charges
        // - Quelle ist EnemyProjectile (Tag oder optional Layer)
        if (type == DamageType.Range && shieldCharges > 0)
        {
            bool tagOk = !string.IsNullOrEmpty(shieldBlocksOnlyTag) && sourceTag == shieldBlocksOnlyTag;
            bool layerOk = (shieldBlocksOnlyLayer >= 0) && (sourceLayer == shieldBlocksOnlyLayer);

            if (tagOk || layerOk)
            {
                shieldCharges = Mathf.Max(0, shieldCharges - 1);
                Debug.Log($"[PlayerHealth] Range-Schaden geblockt durch Shield (EnemyProjectile). Rest: {shieldCharges}/{maxShieldCharges}");

                heartUIManager?.UpdateShield(shieldCharges);
                return; // kein Damage, keine Invincibility
            }
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
        yield return new WaitForSeconds(invincibilityDuration);
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

        if (prefab == null)
            return;

        Vector3 spawnPos = transform.position + Vector3.up * 1f;

        GameObject vfx = Instantiate(
            prefab,
            spawnPos,
            Quaternion.Euler(90f, Random.Range(0f, 360f), 0f)
        );
        Destroy(vfx, 1f);
    }

    private void PlayRandomDamageSound()
    {
        AudioClip[] clips = new AudioClip[] { damageSound1, damageSound2, damageSound3 };

        var valid = new System.Collections.Generic.List<AudioClip>();
        foreach (var c in clips)
            if (c != null) valid.Add(c);

        if (valid.Count == 0) return;

        int index = Random.Range(0, valid.Count);
        AudioManager.Instance.PlaySound2D(valid[index]);
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;

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

    // ✅ Gibt Shield auf einen definierten Wert (0..max)
    public void GiveShield(int charges)
    {
        shieldCharges = Mathf.Clamp(charges, 0, maxShieldCharges);
        Debug.Log($"[PlayerHealth] Shield gesetzt: {shieldCharges}/{maxShieldCharges}");

        heartUIManager?.UpdateShield(shieldCharges);
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Spieler gestorben – Objekt deaktiviert.");
        StartCoroutine(PlayDeathSequence());
    }

    private IEnumerator PlayDeathSequence()
    {
        if (deathSound_1 != null)
            AudioManager.Instance.PlaySound2D(deathSound_1);

        yield return new WaitForSeconds(0.1f);

        if (deathSound_2 != null)
            AudioManager.Instance.PlaySound2D(deathSound_2);

        yield return new WaitForSeconds(0.5f);

        gameObject.SetActive(false);
    }
}
