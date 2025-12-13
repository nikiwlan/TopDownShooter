using UnityEngine;
using System.Collections; // HINZUGEFÜGT: Notwendig für Coroutinen und IEnumerator

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
    public int maxShieldCharges = 4;
    [SerializeField] private int shieldCharges = 0; // 0 = kein Schild

    public bool HasShield => shieldCharges > 0;
    public int ShieldCharges => shieldCharges;


    // HINZUGEFÜGT: Neue Header-Sektion für die Unverwundbarkeit
    [Header("Invincibility Settings")]
    public float invincibilityDuration = 0.5f; // Dauer der Unverwundbarkeit in Sekunden
    private bool isInvincible = false; // Flag, das den Status speichert

    [Header("UI References")]
    public HeartUIManager heartUIManager;
    public DamageFlash damageFlash;

    // ... (Audio und VFX Header bleiben unverändert) ...
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
    public GameObject rangeBloodVFX;   // Blut bei Fernkampfschaden
    public GameObject meleeBloodVFX;   // Blut bei Nahkampfschaden

    [HideInInspector]
    public DamageType lastDamageType = DamageType.Range; // Default

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        if (heartUIManager != null)
        {
            heartUIManager.UpdateHearts(currentHealth);
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
    public void TakeDamage(int amount)
    {
        // HINZUGEFÜGT: Prüfe, ob der Spieler gerade unverwundbar ist
        if (isInvincible)
        {
            Debug.Log("[PlayerHealth] Schaden geblockt dank Grace Period.");
            return; // Beende die Funktion hier, der Spieler nimmt keinen Schaden
        }

        // ✅ Schild blockt NUR Range
        if (lastDamageType == DamageType.Range && shieldCharges > 0)
        {
            shieldCharges--;
            Debug.Log($"[PlayerHealth] Range-Schaden geblockt durch Shield. Rest: {shieldCharges}/{maxShieldCharges}");

            // UI updaten
            if (heartUIManager != null)
                heartUIManager.UpdateShield(shieldCharges);

            return; // kein Damage, keine Invincibility starten
        }


        // HINZUGEFÜGT: Starte die Coroutine für die Unverwundbarkeitsphase
        StartCoroutine(BecomeTemporarilyInvincible());

        // Screen-Flash
        if (damageFlash != null)
            damageFlash.Flash();

        // Blut-Effekt (Range / Melee)
        SpawnBloodVFX();

        // if (currentHealth <= 0) return; // Diese Zeile ist jetzt unnötig, da wir oben prüfen

        int before = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        Debug.Log($"[PlayerHealth] Schaden: {before} → {currentHealth}");


        heartUIManager?.UpdateHearts(currentHealth);

        PlayRandomDamageSound();

        if (currentHealth <= 0)
            Die();
    }

    // HINZUGEFÜGT: Die Coroutine, die die Unverwundbarkeit steuert
    private IEnumerator BecomeTemporarilyInvincible()
    {
        isInvincible = true;
        // Optional: Hier könnten Sie das SpriteRenderer blinken lassen, um Feedback zu geben

        // Warte für die Dauer, die im Inspector eingestellt ist
        yield return new WaitForSeconds(invincibilityDuration);

        isInvincible = false;
        // Optional: Hier das Blinken stoppen
    }


    // ------------------------------------------------------------
    // BLOOD VFX (wie beim Tank, nur mit Range/Melee-Auswahl)
    // ------------------------------------------------------------
    // ... (Rest der Funktionen SpawnBloodVFX, PlayRandomDamageSound, Heal, Die, PlayDeathSequence) ...
    // Diese Funktionen wurden nicht verändert.

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

        Vector3 spawnPos =
            transform.position +
            Vector3.up * 1f;

        GameObject vfx = Instantiate(
            prefab,
            spawnPos,
            Quaternion.Euler(90f, Random.Range(0f, 360f), 0f)
        );
        Destroy(vfx, 1f);
    }

    // ------------------------------------------------------------
    // AUDIO
    // ------------------------------------------------------------
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

    // ------------------------------------------------------------
    // HEAL
    // ------------------------------------------------------------
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

    public void GiveShield(int charges)
    {
        shieldCharges = Mathf.Clamp(charges, 0, maxShieldCharges);
        Debug.Log($"[PlayerHealth] Shield gesetzt: {shieldCharges}/{maxShieldCharges}");

        // UI updaten, wenn vorhanden
        if (heartUIManager != null)
            heartUIManager.UpdateShield(shieldCharges);
    }

    // ------------------------------------------------------------
    // DEATH
    // ------------------------------------------------------------
    private void Die()
    {
        Debug.Log("[PlayerHealth] Spieler gestorben – Objekt deaktiviert.");
        StartCoroutine(PlayDeathSequence());
    }

    private System.Collections.IEnumerator PlayDeathSequence()
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
