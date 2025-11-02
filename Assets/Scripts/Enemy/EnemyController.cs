using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float detectionRange = 10f;

    private Rigidbody rb;
    private Transform player;

    private bool isSlowed = false;
    private Coroutine slowRoutine;
    private float baseSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = false;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        baseSpeed = moveSpeed; // Originalgeschwindigkeit speichern
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Folge dem Spieler, wenn er in Reichweite ist
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= detectionRange)
        {
            Vector3 move = direction.normalized * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);

            // Gegner schaut zum Spieler
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 0.2f));
            }
        }
    }

    // ============================================================
    // 🕓 TIME SLOW POWER-UP (NEUE VERSION)
    // ============================================================
    public void ApplyTimeSlow(float duration, float slowFactor)
    {
        if (slowRoutine != null) StopCoroutine(slowRoutine);
        slowRoutine = StartCoroutine(TimeSlowRoutine(duration, slowFactor));
    }

    private IEnumerator TimeSlowRoutine(float duration, float slowFactor)
    {
        isSlowed = true;
        moveSpeed = baseSpeed * slowFactor;

        // Optional: Farbe ändern, um den Effekt sichtbar zu machen
        Renderer rend = GetComponentInChildren<Renderer>();
        Color originalColor = rend ? rend.material.color : Color.white;
        if (rend) rend.material.color = Color.cyan;

        Debug.Log($"[EnemyController] Gegner verlangsamt auf {moveSpeed}");

        float timer = duration;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        // Geschwindigkeit und Farbe wiederherstellen
        moveSpeed = baseSpeed;
        if (rend) rend.material.color = originalColor;
        isSlowed = false;
        slowRoutine = null;

        Debug.Log("[EnemyController] TimeSlow abgelaufen – Gegner normal.");
    }
}
