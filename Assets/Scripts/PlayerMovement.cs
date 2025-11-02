using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Standardgeschwindigkeit des Spielers")]
    public float moveSpeed = 4f;

    [Header("Speed Boost Settings")]
    [Tooltip("Partikeleffekt beim SpeedBoost (optional)")]
    public GameObject speedBoostEffect;
    [Tooltip("Soundeffekt beim Start des Boosts (optional)")]
    public AudioClip boostSound;

    private Rigidbody rb;
    private Vector3 input;
    private Vector3 moveDirection;
    private bool isSpeedBoostActive = false;
    private Coroutine speedBoostRoutine;

    private AudioSource audioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Eingaben erfassen (WASD oder Pfeiltasten)
        input.x = Input.GetAxisRaw("Horizontal");
        input.z = Input.GetAxisRaw("Vertical");
        input.y = 0f;

        // Diagonale normalisieren
        moveDirection = input.normalized;
    }

    void FixedUpdate()
    {
        // Bewegung in XZ-Ebene
        Vector3 velocity = moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + velocity);

        // Spieler schaut in Bewegungsrichtung
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.2f));
        }
    }

    // ================================================================
    // ⚡ SPEED BOOST SYSTEM
    // ================================================================

    /// <summary>
    /// Aktiviert einen temporären SpeedBoost.
    /// </summary>
    /// <param name="duration">Dauer in Sekunden</param>
    /// <param name="multiplier">Faktor, um den moveSpeed multipliziert wird</param>
    public void ApplySpeedBoost(float duration, float multiplier)
    {
        if (speedBoostRoutine != null)
            StopCoroutine(speedBoostRoutine);

        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(duration, multiplier));
    }

    private IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        if (isSpeedBoostActive) yield break;

        isSpeedBoostActive = true;
        float originalSpeed = moveSpeed;
        moveSpeed *= multiplier;

        // Partikeleffekt aktivieren
        GameObject effect = null;
        if (speedBoostEffect != null)
        {
            effect = Instantiate(speedBoostEffect, transform.position, Quaternion.identity, transform);
        }

        // Sound abspielen
        if (boostSound != null)
        {
            audioSource.PlayOneShot(boostSound);
        }

        Debug.Log($"[PlayerMovement] SpeedBoost aktiviert für {duration:F1}s (x{multiplier})");

        // Dauer warten
        yield return new WaitForSeconds(duration);

        // Boost zurücksetzen
        moveSpeed = originalSpeed;
        isSpeedBoostActive = false;

        if (effect != null)
            Destroy(effect);

        Debug.Log("[PlayerMovement] SpeedBoost beendet");
    }
}
