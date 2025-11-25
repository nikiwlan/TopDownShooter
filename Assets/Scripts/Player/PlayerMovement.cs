using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public LayerMask wallLayer;
    public LayerMask gateLayer;

    [Header("Speed Boost Settings")]
    public GameObject speedBoostEffect;
    public AudioClip boostSound;

    [Header("Footstep Settings")]
    public AudioClip footstepSound;
    [Range(0f, 1f)] public float footstepVolume = 0.65f;

    [Header("PowerUp Sounds")]
    public AudioClip genericPowerUpSound;   // FireRate + ScoreBoost + TimeSlow + SpeedBoost
    public AudioClip healPowerUpSound;      // Hearts

    [Header("Collision Tuning")]
    public float castSkin = 0.2f;
    public float shrinkXZ = 0.02f;
    public float halfExtentY = 0.2f;

    public bool isFrozen = false;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;

    private bool isSpeedBoostActive = false;
    private Coroutine speedBoostRoutine;

    private AudioSource movementSource;
    private AudioSource sfxSource;

    private Vector3 moveDirection;
    private Vector3 lastMoveDirection;

    private BoxCollider col;

    void Awake()
    {
        col = GetComponent<BoxCollider>();

        movementSource = gameObject.AddComponent<AudioSource>();
        movementSource.loop = true;
        movementSource.playOnAwake = false;
        movementSource.spatialBlend = 0f;
        movementSource.volume = footstepVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveDirection = new Vector3(h, 0f, v).normalized;

        if (moveDirection.sqrMagnitude > 0.01f)
            lastMoveDirection = moveDirection;

        TryMove(moveDirection);

        HandleAnimator();
        HandleFootstepSound();

        if (isFrozen)
        {
            movementSource.Stop();
            return;
        }
    }

    // ------------------------------------------------------------
    // ⭐ Laufgeräusch
    // ------------------------------------------------------------
    private void HandleFootstepSound()
    {
        bool isMoving = moveDirection.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            if (!movementSource.isPlaying && footstepSound != null)
            {
                movementSource.clip = footstepSound;
                movementSource.volume = footstepVolume;
                movementSource.Play();
            }
        }
        else
        {
            if (movementSource.isPlaying)
                movementSource.Stop();
        }
    }

    // ------------------------------------------------------------
    // Animation
    // ------------------------------------------------------------
    private void HandleAnimator()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", moveDirection.magnitude);

            if (moveDirection.sqrMagnitude > 0.01f && !Input.GetMouseButton(0))
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirection);
                animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, targetRot, 0.2f);
            }
        }
    }

    // ------------------------------------------------------------
    // Bewegung + Sliding (unverändert)
    // ------------------------------------------------------------
    private void TryMove(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        Vector3 halfExtents = Vector3.Scale(col.size * 0.5f, transform.lossyScale);
        halfExtents = new Vector3(
            Mathf.Max(halfExtents.x - shrinkXZ, 0.005f),
            Mathf.Max(halfExtentY, 0.005f),
            Mathf.Max(halfExtents.z - shrinkXZ, 0.005f)
        );

        Quaternion orient = transform.rotation;
        float step = moveSpeed * Time.deltaTime;

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

        Vector3 pos = transform.position;
        Vector3 move = direction;
        float remaining = step;

        for (int iter = 0; iter < 2 && remaining > 0.0001f; iter++)
        {
            centerNow = col.bounds.center;

            bool hitSomething = false;
            float hitDist = Mathf.Infinity;
            Vector3 hitNormal = Vector3.zero;

            if (Physics.BoxCast(centerNow, halfExtents, move, out RaycastHit wallHit, orient,
                                remaining + castSkin, wallLayer, QueryTriggerInteraction.Ignore))
            {
                hitSomething = true;
                hitDist = wallHit.distance;
                hitNormal = wallHit.normal;
            }

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
                float allowed = Mathf.Max(hitDist - castSkin, 0f);
                if (allowed > 0f)
                {
                    pos += move * allowed;
                    transform.position = pos;
                }

                Vector3 n = hitNormal; n.y = 0f; n.Normalize();
                Vector3 slide = Vector3.ProjectOnPlane(move, n).normalized;

                if (slide.sqrMagnitude < 1e-4f) break;

                remaining -= allowed;
                move = slide;
            }
            else
            {
                pos += move * remaining;
                transform.position = pos;
                remaining = 0f;
            }
        }
    }

    // ------------------------------------------------------------
    // Trigger-Reaktionen
    // ------------------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PowerUp")) return;

        string name = other.name.ToLower();

        // ⭐ Heart / Heal
        if (name.Contains("heart") || name.Contains("heal"))
        {
            if (healPowerUpSound != null)
                sfxSource.PlayOneShot(healPowerUpSound);
        }
        else
        {
            // ⭐ Alle anderen PowerUps → derselbe Sound
            if (genericPowerUpSound != null)
                sfxSource.PlayOneShot(genericPowerUpSound);
        }
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

    private IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        if (isSpeedBoostActive) yield break;
        isSpeedBoostActive = true;

        float original = moveSpeed;
        moveSpeed *= multiplier;

        GameObject fx = null;
        if (speedBoostEffect) fx = Instantiate(speedBoostEffect, transform.position, Quaternion.identity, transform);

        if (boostSound && sfxSource != null)
            sfxSource.PlayOneShot(boostSound);

        yield return new WaitForSeconds(duration);

        moveSpeed = original;
        if (fx) Destroy(fx);
        isSpeedBoostActive = false;
    }

    public AudioSource GetSfxSource() => sfxSource;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, lastMoveDirection * 0.8f);
    }
#endif
}
