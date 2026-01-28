using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public float height = 10f;
    public float distance = 0f;
    public float smoothSpeed = 10f;

    [Header("Shake Configuration")]
    // HIER stellst du jetzt ein, wie es wackelt bei Schaden
    public float damageShakeDuration = 0.2f;
    public float damageShakeMagnitude = 0.5f;

    // Diese Variablen siehst du im Inspector nur zum Debuggen (SerializeField)
    [Header("Debug Runtime")]
    [SerializeField] private float shakeTimer = 0f;
    [SerializeField] private float shakeMagnitude = 0f;
    private float dampingSpeed = 1.0f;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Position berechnen
        Vector3 targetPos = new Vector3(target.position.x, height, target.position.z - distance);
        Vector3 finalPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        // 2. Shake addieren
        if (shakeTimer > 0)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.y = 0;
            finalPos += shakeOffset;
            shakeTimer -= Time.deltaTime * dampingSpeed;
        }
        else
        {
            shakeTimer = 0f;
        }

        transform.position = finalPos;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // --- NEU: Die Methode für PlayerHealth (KEINE PARAMETER) ---
    public void TriggerDamageShake()
    {
        // Die Kamera nutzt ihre EIGENEN Einstellungen
        shakeTimer = damageShakeDuration;
        shakeMagnitude = damageShakeMagnitude;
        dampingSpeed = 1.0f;
    }
}