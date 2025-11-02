using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public LayerMask wallLayer; // Layer für Wände

    [Header("Speed Boost Settings")]
    public GameObject speedBoostEffect;
    public AudioClip boostSound;

    private bool isSpeedBoostActive = false;
    private Coroutine speedBoostRoutine;
    private AudioSource audioSource;

    // interne Bewegung
    private Vector3 moveDirection;
    private Vector3 lastMoveDirection;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // Eingabe abfragen
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector3(h, 0f, v).normalized;

        if (moveDirection.sqrMagnitude > 0.01f)
            lastMoveDirection = moveDirection;

        // Bewegung prüfen
        TryMove(moveDirection);
    }

    private void TryMove(Vector3 direction)
    {
        if (direction == Vector3.zero)
            return;

        Vector3 move = direction * moveSpeed * Time.deltaTime;
        Vector3 startPos = transform.position + Vector3.up * 0.5f; // leicht über Boden

        // Kollision prüfen (Raycast)
        if (!Physics.Raycast(startPos, direction, out RaycastHit hit, move.magnitude + 0.2f, wallLayer))
        {
            transform.position += move;
        }
        else
        {
            // Debug-Ausgabe, wenn geblockt
            Debug.DrawRay(startPos, direction * hit.distance, Color.red, 0.1f);
        }

        // Rotation
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
    }

    void OnTriggerEnter(Collider other)
    {
        // PowerUps
        if (other.CompareTag("PowerUp"))
        {
            Debug.Log("[PlayerMovement] PowerUp getriggert: " + other.name);
            // Hier z. B. Effekt aktivieren oder Script aufrufen
        }

        // Gegner
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("[PlayerMovement] Gegnerkontakt mit " + other.name);
            PlayerHealth hp = GetComponent<PlayerHealth>();
            if (hp != null)
                hp.TakeDamage(1);
        }
    }

    // ⚡ SPEED BOOST SYSTEM
    public void ApplySpeedBoost(float duration, float multiplier)
    {
        if (speedBoostRoutine != null)
            StopCoroutine(speedBoostRoutine);

        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(duration, multiplier));
    }

    private System.Collections.IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        if (isSpeedBoostActive) yield break;
        isSpeedBoostActive = true;

        float originalSpeed = moveSpeed;
        moveSpeed *= multiplier;

        GameObject effect = null;
        if (speedBoostEffect != null)
            effect = Instantiate(speedBoostEffect, transform.position, Quaternion.identity, transform);

        if (boostSound != null)
            audioSource.PlayOneShot(boostSound);

        yield return new WaitForSeconds(duration);

        moveSpeed = originalSpeed;
        if (effect != null)
            Destroy(effect);

        isSpeedBoostActive = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Visualisiert den Wand-Ray
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, lastMoveDirection * 0.8f);
    }
#endif
}
