// BossBeetleContext.cs
using UnityEngine;
using System.Collections;

public interface IBossBeetlePhase
{
    void Enter(bool triggerRage);
    void Exit();
    void Tick(bool closeForRun);
    void OnHeadHit(int damage, Vector3 hitDir, Vector3 hitPoint);
}

public sealed class BossBeetleContext
{
    public readonly BossBeetle Owner;
    public readonly Transform PlayerTransform;

    // Runtime flags
    public bool IsRunning { get; private set; }
    public bool IsStunned { get; private set; }
    public bool IsRaging { get; private set; }
    public bool IsAttacking { get; private set; }

    // Run runtime
    private float _runEndTime;
    private Vector3 _runDir;
    private float _nextRunAllowedTime;

    // Stun runtime
    private float _stunEndTime;
    private GameObject _stunIconInstance;

    // Rage runtime
    private float _rageEndTime = -1f;

    // Attack runtime
    private float _nextAttackTime = 0f;

    // Phase2 Jump runtime
    private float _nextJumpAllowedTime = 0f;
    private bool _isJumping = false;

    public bool IsValid => (Owner != null && Owner.animator != null && PlayerTransform != null);

    public BossBeetleContext(BossBeetle owner, Transform playerTransform)
    {
        Owner = owner;
        PlayerTransform = playerTransform;
    }

    // ==========================
    // GLOBAL GATES (wie in Update)
    // ==========================
    public bool TickCoreGates()
    {
        // STUN
        if (IsStunned)
        {
            if (Time.time >= _stunEndTime)
                EndStun();

            Owner.animator.SetFloat("Speed", 0f);
            return true;
        }

        // RAGE TIMER
        if (IsRaging && Time.time >= _rageEndTime)
            IsRaging = false;

        // RAGE BLOCK
        if (IsRaging)
        {
            if (IsRunning) StopRunAndCooldown(0f);
            IsAttacking = false;
            Owner.animator.SetFloat("Speed", 0f);
            return true;
        }

        // ATTACK BLOCK
        if (IsAttacking)
        {
            Owner.animator.SetFloat("Speed", 0f);
            return true;
        }

        return false;
    }

    // ==========================
    // PHASE ENTER HELPERS
    // ==========================
    public void StartRage()
    {
        IsRaging = true;
        _rageEndTime = Time.time + Owner.rageDuration;

        // alles stoppen (wie bei dir)
        IsAttacking = false;
        if (IsRunning) StopRunAndCooldown(0f);

        Owner.animator.ResetTrigger(BossBeetle.TRIG_ATTACK);
        Owner.animator.SetTrigger(BossBeetle.TRIG_RAGE);
        Owner.animator.SetFloat("Speed", 0f);
    }

    // ==========================
    // RUN (shared Phase1/2)
    // ==========================
    public bool CanStartRunNow() => Time.time >= _nextRunAllowedTime;

    public void TryStartRun(bool closeForRun)
    {
        if (!closeForRun) return;
        if (IsRunning || IsAttacking) return;
        if (!CanStartRunNow()) return;

        StartRun();
    }

    private void StartRun()
    {
        IsRunning = true;
        _runEndTime = Time.time + Owner.runMaxTime;

        Vector3 toPlayer = PlayerTransform.position - Owner.transform.position;
        toPlayer.y = 0f;
        _runDir = (toPlayer.sqrMagnitude > 0.0001f) ? toPlayer.normalized : Owner.transform.forward;

        ApplyAnimatorRunFlag();
    }

    public void StopRunAndCooldown(float cooldownSeconds)
    {
        IsRunning = false;
        ApplyAnimatorRunFlag();

        Owner.StartCoroutine(ForceLeaveRunOneFrame());
        _nextRunAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
    }

    private IEnumerator ForceLeaveRunOneFrame()
    {
        bool wasClose = Owner.animator.GetBool(BossBeetle.PARAM_CLOSE);
        Owner.animator.SetBool(BossBeetle.PARAM_CLOSE, false);
        yield return null;
        Owner.animator.SetBool(BossBeetle.PARAM_CLOSE, wasClose);
    }

    public void RunMove()
    {
        if (!PlayerTransform) return;

        // Zielpunkt = nächster Punkt auf Player-Hitbox (wie bei dir)
        Vector3 playerPoint = PlayerTransform.position;
        if (Owner.playerBodyCollider != null)
            playerPoint = Owner.playerBodyCollider.ClosestPoint(Owner.transform.position);

        Vector3 toTarget = playerPoint - Owner.transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        // StopDist = max(runStopDistance, bossRadius + padding)
        float bossRadius = 0f;
        if (Owner.bossBodyCollider != null)
        {
            Vector3 e = Owner.bossBodyCollider.bounds.extents;
            bossRadius = Mathf.Max(e.x, e.z);
        }

        float desiredStopDist = Mathf.Max(Owner.runStopDistance, bossRadius + Owner.stopPadding);

        if (Time.time >= _runEndTime || dist <= desiredStopDist)
        {
            StopRunAndCooldown(Owner.runCooldownAfterRun);
            return;
        }

        // Wall check
        if (Physics.Raycast(Owner.transform.position + Vector3.up * 0.2f, _runDir, Owner.runWallCheckDistance, Owner.wallLayer))
        {
            StopRunAndCooldown(0f);
            StartStun();
            return;
        }

        // Steering Richtung zum Player-Rand
        Vector3 targetDir = toTarget;
        if (targetDir.sqrMagnitude > 0.0001f) targetDir.Normalize();
        else targetDir = _runDir;

        float t = Mathf.Clamp01(dist / Owner.runSteerDistanceRange);
        float steerMul = Mathf.Lerp(Owner.runSteerNearMultiplier, Owner.runSteerFarMultiplier, t);
        float steer = Owner.runSteerStrength * steerMul;

        _runDir = Vector3.Slerp(_runDir, targetDir, steer * Time.deltaTime).normalized;

        float step = Owner.runSpeed * Time.deltaTime;
        float allowedStep = Mathf.Min(step, dist - desiredStopDist);

        if (allowedStep > 0f)
        {
            if (!Physics.Raycast(Owner.transform.position, _runDir, allowedStep + 0.2f, Owner.wallLayer))
                Owner.transform.position += _runDir * allowedStep;
        }

        if (_runDir.sqrMagnitude > 0.001f)
        {
            Owner.transform.rotation = Quaternion.Slerp(
                Owner.transform.rotation,
                Quaternion.LookRotation(_runDir),
                0.25f
            );
        }
    }

    public void ApplyAnimatorRunFlag()
    {
        Owner.animator.SetBool(BossBeetle.PARAM_ISRUN, IsRunning);
    }

    // ==========================
    // STUN
    // ==========================
    public void StartStun()
    {
        IsStunned = true;
        _stunEndTime = Time.time + Owner.wallStunDuration;

        if (Owner.stunIconPrefab && _stunIconInstance == null)
            _stunIconInstance = Object.Instantiate(Owner.stunIconPrefab, Owner.stunIconAnchor.position, Quaternion.identity, Owner.stunIconAnchor);
    }

    private void EndStun()
    {
        IsStunned = false;

        if (_stunIconInstance)
        {
            Object.Destroy(_stunIconInstance);
            _stunIconInstance = null;
        }

        _nextRunAllowedTime = Time.time + Mathf.Max(0f, Owner.runCooldownAfterStun);
    }

    // ==========================
    // MOVE (walk)
    // ==========================
    public void MoveTowardsPlayer(float speed, Transform origin)
    {
        if (!PlayerTransform) return;

        Vector3 playerPoint = PlayerTransform.position;

        if (Owner.playerBodyCollider != null)
            playerPoint = Owner.playerBodyCollider.ClosestPoint(origin.position);

        Vector3 dir = (playerPoint - origin.position);
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return;
        dir /= dist;

        float bossRadius = 0f;
        if (Owner.bossBodyCollider != null)
        {
            Vector3 e = Owner.bossBodyCollider.bounds.extents;
            bossRadius = Mathf.Max(e.x, e.z);
        }

        float desiredStopDist = bossRadius + Owner.stopPadding;
        if (dist <= desiredStopDist) return;

        float step = speed * Time.deltaTime;
        float allowedStep = Mathf.Min(step, dist - desiredStopDist);

        if (!Physics.Raycast(Owner.transform.position, dir, allowedStep + 0.2f, Owner.wallLayer))
            Owner.transform.position += dir * allowedStep;

        Owner.transform.rotation = Quaternion.Slerp(
            Owner.transform.rotation,
            Quaternion.LookRotation(dir),
            0.2f
        );
    }

    // ==========================
    // ATTACK (shared)
    // ==========================
    public Transform GetOriginForPhase(int phase)
    {
        return phase switch
        {
            0 => Owner.attackOrigin1,
            1 => Owner.attackOrigin2,
            _ => Owner.attackOrigin3
        };
    }

    public float GetAttackRangeForPhase(int phase)
    {
        return phase switch
        {
            0 => Owner.attackRange1,
            1 => Owner.attackRange2,
            _ => Owner.attackRange3
        };
    }

    public int GetAttackDamageForPhase(int phase)
    {
        return phase switch
        {
            0 => Owner.attackDamage1,
            1 => Owner.attackDamage2,
            _ => Owner.attackDamage3
        };
    }

    public float GetAttackDurationForPhase(int phase)
    {
        return phase switch
        {
            0 => Owner.attackDuration1,
            1 => Owner.attackDuration2,
            _ => Owner.attackDuration3
        };
    }

    public float GetAttackCooldownForPhase(int phase)
    {
        return phase switch
        {
            0 => Owner.attackCooldown1,
            1 => Owner.attackCooldown2,
            _ => Owner.attackCooldown3
        };
    }

    public bool CanAttackNow() => Time.time >= _nextAttackTime;

    public void TryStartAttack(int phase)
    {
        if (IsAttacking) return;
        if (!CanAttackNow()) return;

        Owner.StartCoroutine(AttackRoutine(phase));
    }

    private IEnumerator AttackRoutine(int phase)
    {
        AbortRunForAttack();

        IsAttacking = true;
        Owner.animator.SetTrigger(BossBeetle.TRIG_ATTACK);

        float dur = GetAttackDurationForPhase(phase);
        yield return new WaitForSeconds(dur * 0.5f);

        if (Owner.attackHitSound)
            AudioManager.Instance.PlaySound3D(Owner.attackHitSound, Owner.transform.position);

        Owner.player?.TakeDamage(GetAttackDamageForPhase(phase));

        yield return new WaitForSeconds(dur * 0.5f);

        IsAttacking = false;
        _nextAttackTime = Time.time + GetAttackCooldownForPhase(phase);
    }

    private void AbortRunForAttack()
    {
        if (IsRunning)
            StopRunAndCooldown(0f);

        Owner.animator.SetFloat("Speed", 0f);
    }

    // ==========================
    // PHASE 2: JUMP ATTACK
    // ==========================
    public bool CanJumpNow() => Time.time >= _nextJumpAllowedTime && !_isJumping;

    public void TryStartJumpAttack()
    {
        if (!CanJumpNow()) return;
        if (IsAttacking || IsStunned || IsRaging) return;

        Owner.StartCoroutine(JumpAttackRoutine());
    }

    private IEnumerator JumpAttackRoutine()
    {
        // Jump blockt Movement/Attack
        _isJumping = true;
        IsAttacking = true;

        // optional: Run abbrechen
        if (IsRunning) StopRunAndCooldown(0f);

        Vector3 start = Owner.transform.position;

        // Zielpunkt: nahe am Player (ClosestPoint)
        Vector3 target = PlayerTransform.position;
        if (Owner.playerBodyCollider != null)
            target = Owner.playerBodyCollider.ClosestPoint(start);

        target.y = start.y; // wir bewegen nur horizontal; y kommt aus Arc

        float duration = Mathf.Max(0.1f, Owner.jumpDuration);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = Mathf.Clamp01(t);

            // Horizontal Lerp
            Vector3 pos = Vector3.Lerp(start, target, eased);

            // Parabel-Arc
            float arc = 4f * Owner.jumpHeight * eased * (1f - eased);
            pos.y += arc;

            Owner.transform.position = pos;

            // Face Richtung target
            Vector3 faceDir = (target - Owner.transform.position);
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.001f)
            {
                Owner.transform.rotation = Quaternion.Slerp(
                    Owner.transform.rotation,
                    Quaternion.LookRotation(faceDir.normalized),
                    0.35f
                );
            }

            yield return null;
        }

        // Landung: AoE Damage wenn Player in Radius
        if (Owner.player != null)
        {
            float dist = Vector3.Distance(Owner.transform.position, PlayerTransform.position);
            if (dist <= Owner.jumpLandingRadius)
                Owner.player.TakeDamage(Owner.jumpLandingDamage);
        }

        // Cooldown
        _nextJumpAllowedTime = Time.time + Mathf.Max(0f, Owner.jumpCooldown);

        _isJumping = false;
        IsAttacking = false;
    }

    // ==========================
    // FORCE STOP
    // ==========================
    public void ForceStopAll()
    {
        // Run aus
        IsRunning = false;
        ApplyAnimatorRunFlag();

        // Stun cleanup
        if (IsStunned)
        {
            IsStunned = false;
            if (_stunIconInstance)
            {
                Object.Destroy(_stunIconInstance);
                _stunIconInstance = null;
            }
        }

        // Rage/Attack
        IsRaging = false;
        IsAttacking = false;
    }
}
