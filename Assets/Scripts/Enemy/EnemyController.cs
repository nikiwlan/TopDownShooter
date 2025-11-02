using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CapsuleCollider))]
public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float detectionRange = 10f;
    public LayerMask wallLayer;

    private Transform player;
    private float baseSpeed;
    private bool isSlowed = false;
    private Coroutine slowRoutine;
    private Renderer rend;
    private Color originalColor;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        baseSpeed = moveSpeed;
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) originalColor = rend.material.color;
    }

    void Update()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position);
        direction.y = 0f;

        if (direction.magnitude <= detectionRange)
        {
            Vector3 move = direction.normalized * moveSpeed * Time.deltaTime;

            // Vermeide, in Wände zu laufen
            if (!Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, move.magnitude + 0.1f, wallLayer))
            {
                transform.position += move;
            }

            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
            }
        }
    }

    // 🕓 TIME SLOW
    public void ApplyTimeSlow(float duration, float slowFactor)
    {
        if (slowRoutine != null)
            StopCoroutine(slowRoutine);

        slowRoutine = StartCoroutine(TimeSlowRoutine(duration, slowFactor));
    }

    private IEnumerator TimeSlowRoutine(float duration, float slowFactor)
    {
        if (isSlowed) yield break;

        isSlowed = true;
        moveSpeed = baseSpeed * slowFactor;
        Debug.Log($"[EnemyController] Time Slow aktiviert für {duration}s mit Faktor {slowFactor}");

        if (rend) rend.material.color = Color.cyan;

        float timer = duration;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        moveSpeed = baseSpeed;
        if (rend) rend.material.color = originalColor;
        isSlowed = false;
        slowRoutine = null;

        Debug.Log("[EnemyController] Time Slow beendet, normale Geschwindigkeit wiederhergestellt.");
    }
}
