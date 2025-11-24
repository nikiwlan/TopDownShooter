using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;

    [Header("UI References")]
    public HeartUIManager heartUIManager;

    // ---------------------- AUDIO ----------------------
    [Header("Damage Sounds (Randomized)")]
    public AudioClip damageSound1;
    public AudioClip damageSound2;
    public AudioClip damageSound3;

    [Header("Death Sounds (Played In Order)")]
    public AudioClip deathSound_1;      // Stöhnen
    public AudioClip deathSound_2;      // Body fall

    private AudioSource audioSource;

    void Awake()
    {
        currentHealth = maxHealth;

        // AudioSource erzeugen, falls keiner vorhanden
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;  // 2D Sound
        }
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
        if (currentHealth <= 0) return;

        int before = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        Debug.Log($"[PlayerHealth] Schaden: {before} → {currentHealth}");

        heartUIManager?.UpdateHearts(currentHealth);

        // ------ RANDOM DAMAGE SOUND ------
        PlayRandomDamageSound();

        if (currentHealth <= 0)
            Die();
    }

    // Spielt zufälligen Schaden-Sound
    private void PlayRandomDamageSound()
    {
        AudioClip[] clips = new AudioClip[] { damageSound1, damageSound2, damageSound3 };

        // Filtere leere Slots raus
        var valid = new System.Collections.Generic.List<AudioClip>();
        foreach (var c in clips)
            if (c != null) valid.Add(c);

        if (valid.Count == 0) return;  // nix drin → nix spielen

        int index = Random.Range(0, valid.Count);
        audioSource.PlayOneShot(valid[index]);
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
        // Sound 1: Stöhnen
        if (deathSound_1 != null)
            audioSource.PlayOneShot(deathSound_1);

        // mini Delay, damit Sound 2 nicht gleichzeitig kommt
        yield return new WaitForSeconds(0.1f);

        // Sound 2: Body Fall
        if (deathSound_2 != null)
            audioSource.PlayOneShot(deathSound_2);

        // kurze Verzögerung damit der Sound fertig läuft
        yield return new WaitForSeconds(0.5f);

        gameObject.SetActive(false);
    }
}
