using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    [Tooltip("Nicht-Trigger-Collider der Arena/Wände")]
    public LayerMask wallLayer;
    [Tooltip("Trigger-Collider der Gates/Portale")]
    public LayerMask gateLayer;

    [Header("Speed Boost Settings")]
    public GameObject speedBoostEffect;
    public AudioClip boostSound;

    [Header("Collision Tuning")]
    [Tooltip("Sicherheitsabstand vor Kanten")]
    public float castSkin = 0.015f;
    [Tooltip("Schrumpft die Cast-Box in X/Z für feineres Anfühlen")]
    public float shrinkXZ = 0.03f;
    [Tooltip("Höhe der Cast-Box (Top-Down: flach)")]
    public float halfExtentY = 0.2f;

    private bool isSpeedBoostActive = false;
    private Coroutine speedBoostRoutine;
    private AudioSource audioSource;

    private Vector3 moveDirection;
    private Vector3 lastMoveDirection;

    private BoxCollider col;

    void Awake()
    {
        col = GetComponent<BoxCollider>();
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveDirection = new Vector3(h, 0f, v).normalized;

        if (moveDirection.sqrMagnitude > 0.01f)
            lastMoveDirection = moveDirection;

        TryMove(moveDirection);
    }

    // ------------------------------------------------------------
    // Bewegung mit Sliding an Wänden UND Gates (Gate = Trigger!)
    // ------------------------------------------------------------
    private void TryMove(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        // präzise Half-Extents aus Collidergröße
        Vector3 halfExtents = Vector3.Scale(col.size * 0.5f, transform.lossyScale);
        halfExtents = new Vector3(
            Mathf.Max(halfExtents.x - shrinkXZ, 0.005f),
            Mathf.Max(halfExtentY, 0.005f),
            Mathf.Max(halfExtents.z - shrinkXZ, 0.005f)
        );

        Quaternion orient = transform.rotation;
        float step = moveSpeed * Time.deltaTime;

        // 0) Falls wir bereits in einer Wand stecken: sanft heraus schieben
        Vector3 centerNow = col.bounds.center;
        var overlaps = Physics.OverlapBox(centerNow, halfExtents, orient, wallLayer, QueryTriggerInteraction.Ignore);
        foreach (var o in overlaps)
        {
            if (!o || o.isTrigger) continue;
            if (Physics.ComputePenetration(
                    col, transform.position, orient,
                    o, o.transform.position, o.transform.rotation,
                    out Vector3 pushDir, out float pushDist))
            {
                transform.position += pushDir * (pushDist + 0.001f);
            }
        }

        // 1) Bis zu zwei Iterationen: gewünschte Richtung, dann evtl. Slide-Richtung
        Vector3 pos = transform.position;
        Vector3 move = direction;
        float remaining = step;

        for (int iter = 0; iter < 2 && remaining > 0.0001f; iter++)
        {
            centerNow = col.bounds.center;

            // Wir checken Wände (non-trigger) UND Gates (nur trigger) und nehmen den
            // NÄCHSTEN Treffer, damit Ecken korrekt funktionieren.
            bool hitSomething = false;
            float hitDist = Mathf.Infinity;
            Vector3 hitNormal = Vector3.zero;

            // a) Wände
            if (Physics.BoxCast(centerNow, halfExtents, move, out RaycastHit wallHit, orient,
                                remaining + castSkin, wallLayer, QueryTriggerInteraction.Ignore))
            {
                hitSomething = true;
                hitDist = wallHit.distance;
                hitNormal = wallHit.normal;
            }

            // b) Gates (nur Trigger zählen!)
            var gateHits = Physics.BoxCastAll(centerNow, halfExtents, move, orient,
                                              remaining + castSkin, gateLayer, QueryTriggerInteraction.Collide);
            foreach (var gh in gateHits)
            {
                var c = gh.collider;
                if (c && c.isTrigger)
                {
                    if (!hitSomething || gh.distance < hitDist)
                    {
                        hitSomething = true;
                        hitDist = gh.distance;
                        hitNormal = gh.normal;
                    }
                }
            }

            if (hitSomething)
            {
                // Bis kurz vor die Kante bewegen
                float allowed = Mathf.Max(hitDist - castSkin, 0f);
                if (allowed > 0f)
                {
                    pos += move * allowed;
                    transform.position = pos;
                }

                // Slide-Richtung (nur horizontal, damit Top-Down stabil bleibt)
                Vector3 n = hitNormal; n.y = 0f; n.Normalize();
                Vector3 slide = Vector3.ProjectOnPlane(move, n).normalized;

                // Wenn kaum tangentiale Bewegung übrig bleibt -> komplett blockiert
                if (slide.sqrMagnitude < 1e-4f) break;

                remaining -= allowed;
                move = slide;
                continue;
            }
            else
            {
                // Frei: Restdistanz gehen
                pos += move * remaining;
                transform.position = pos;
                remaining = 0f;
            }
        }

        // 2) Ausrichtung nur visuell
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.2f);
    }

    // ------------------------------------------------------------
    // Trigger-Reaktionen
    // ------------------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PowerUp"))
            Debug.Log("[PlayerMovement] PowerUp: " + other.name);

        if (other.CompareTag("Enemy"))
            GetComponent<PlayerHealth>()?.TakeDamage(1);
    }

    // ------------------------------------------------------------
    // Speed Boost
    // ------------------------------------------------------------
    public void ApplySpeedBoost(float duration, float multiplier)
    {
        if (speedBoostRoutine != null)
            StopCoroutine(speedBoostRoutine);
        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(duration, multiplier));
    }

    public void ApplySpeedBoost(float duration) => ApplySpeedBoost(duration, 2f);
    public void ApplySpeedBoost() => ApplySpeedBoost(3f, 2f);

    private System.Collections.IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        if (isSpeedBoostActive) yield break;
        isSpeedBoostActive = true;

        float original = moveSpeed;
        moveSpeed *= multiplier;

        GameObject fx = null;
        if (speedBoostEffect) fx = Instantiate(speedBoostEffect, transform.position, Quaternion.identity, transform);
        if (boostSound) audioSource.PlayOneShot(boostSound);

        yield return new WaitForSeconds(duration);

        moveSpeed = original;
        if (fx) Destroy(fx);
        isSpeedBoostActive = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, lastMoveDirection * 0.8f);
    }
#endif
}
