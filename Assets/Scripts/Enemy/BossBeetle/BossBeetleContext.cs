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

    public bool IsRunning { get; private set; }
    public bool IsStunned { get; private set; }
    public bool IsRaging { get; private set; }
    public bool IsAttacking { get; private set; }

    private float _runEndTime;
    private Vector3 _runDir;
    private float _nextRunAllowedTime;

    private float _stunEndTime;
    private GameObject _stunIconInstance;

    private float _rageEndTime = -1f;

    // Cooldowns
    private float _nextNormalAttackTime = 0f;
    private float _nextSpecialAttackTime = 0f;

    // Global lockout (Recovery)
    private float _nextActionAllowedTime = 0f;

    public bool IsValid => (Owner != null && Owner.animator != null && PlayerTransform != null);

    private bool CanStartAnyActionNow() => Time.time >= _nextActionAllowedTime;

    private void ApplyGlobalRecovery()
    {
        _nextActionAllowedTime = Time.time + Mathf.Max(0f, Owner.actionRecovery);
    }

    public BossBeetleContext(BossBeetle owner, Transform playerTransform)
    {
        Owner = owner;
        PlayerTransform = playerTransform;
    }

    public bool TickCoreGates()
    {
        if (IsStunned)
        {
            if (Time.time >= _stunEndTime)
                EndStun();

            Owner.animator.SetFloat("Speed", 0f);
            return true;
        }

        if (IsRaging && Time.time >= _rageEndTime)
            IsRaging = false;

        if (IsRaging)
        {
            if (IsRunning) StopRunAndCooldown(0f);
            IsAttacking = false;
            Owner.animator.SetFloat("Speed", 0f);
            return true;
        }

        if (IsAttacking)
        {
            Owner.animator.SetFloat("Speed", 0f);
            return true;
        }

        return false;
    }

    public void StartRage()
    {
        IsRaging = true;
        _rageEndTime = Time.time + Owner.rageDuration;

        IsAttacking = false;
        if (IsRunning) StopRunAndCooldown(0f);

        // Safety: Trigger resetten
        Owner.animator.ResetTrigger(BossBeetle.TRIG_ATTACK);
        Owner.animator.ResetTrigger(BossBeetle.TRIG_SPECIAL);

        Owner.animator.SetTrigger(BossBeetle.TRIG_RAGE);
        Owner.animator.SetFloat("Speed", 0f);
    }

    // ==========================
    // RUN
    // ==========================
    public bool CanStartRunNow() => Time.time >= _nextRunAllowedTime;

    public void TryStartRun(bool closeForRun)
    {
        if (!closeForRun) return;
        if (IsRunning || IsAttacking) return;
        if (!CanStartRunNow()) return;
        if (!CanStartAnyActionNow()) return;

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

        ApplyGlobalRecovery();
    }

    private IEnumerator ForceLeaveRunOneFrame()
    {
        bool wasClose = Owner.animator.GetBool(BossBeetle.PARAM_CLOSE);
        Owner.animator.SetBool(BossBeetle.PARAM_CLOSE, false);
        yield return null;
        Owner.animator.SetBool(BossBeetle.PARAM_CLOSE, wasClose);
    }

    public void RunMove(Transform pivot)
    {
        if (!PlayerTransform) return;
        if (pivot == null) pivot = Owner.transform;

        Vector3 playerPoint = PlayerTransform.position;
        if (Owner.playerBodyCollider != null)
            playerPoint = Owner.playerBodyCollider.ClosestPoint(pivot.position);

        Vector3 toTarget = playerPoint - pivot.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        float desiredStopDist = Mathf.Max(Owner.runStopDistance, 0.05f);

        if (Time.time >= _runEndTime || dist <= desiredStopDist)
        {
            StopRunAndCooldown(Owner.runCooldownAfterRun);
            return;
        }

        if (Physics.Raycast(pivot.position + Vector3.up * 0.2f, _runDir, Owner.runWallCheckDistance, Owner.wallLayer))
        {
            StopRunAndCooldown(0f);
            StartStun();
            return;
        }

        Vector3 targetDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : _runDir;

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
    // WALK MOVE
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
    // ATTACK DATA
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

    // ==========================
    // NORMAL ATTACK (Trigger: IsAttacking)
    // ==========================
    public bool CanNormalAttackNow() => Time.time >= _nextNormalAttackTime && CanStartAnyActionNow();

    public void TryStartNormalAttack(int phase)
    {
        if (IsAttacking) return;
        if (!CanNormalAttackNow()) return;

        Owner.StartCoroutine(NormalAttackRoutine(phase));
    }

    private IEnumerator NormalAttackRoutine(int phase)
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
        _nextNormalAttackTime = Time.time + GetAttackCooldownForPhase(phase);
        ApplyGlobalRecovery();
    }

    // ==========================
    // SPECIAL ATTACK (Trigger: SpecialHit)
    // Nutzt hier einfach Phase2-Werte (Range3/Damage3/Duration3/Cooldown3)
    // ==========================
    public bool CanSpecialAttackNow() => Time.time >= _nextSpecialAttackTime && CanStartAnyActionNow();

    public void TryStartSpecialAttack()
    {
        if (IsAttacking) return;
        if (!CanSpecialAttackNow()) return;

        Owner.StartCoroutine(SpecialAttackRoutine());
    }

    private IEnumerator SpecialAttackRoutine()
    {
        // SpecialHit ist für Walk gedacht -> wir stoppen Run, falls gerade noch was läuft
        AbortRunForAttack();

        IsAttacking = true;
        Owner.animator.SetTrigger(BossBeetle.TRIG_SPECIAL);

        // Wir verwenden für Timing/Schaden die Phase2(=3) Stats
        int phase2 = 2;
        float dur = GetAttackDurationForPhase(phase2);

        yield return new WaitForSeconds(dur * 0.5f);

        if (Owner.attackHitSound)
            AudioManager.Instance.PlaySound3D(Owner.attackHitSound, Owner.transform.position);

        Owner.player?.TakeDamage(GetAttackDamageForPhase(phase2));

        yield return new WaitForSeconds(dur * 0.5f);

        IsAttacking = false;
        _nextSpecialAttackTime = Time.time + GetAttackCooldownForPhase(phase2);
        ApplyGlobalRecovery();
    }

    private void AbortRunForAttack()
    {
        if (IsRunning)
            StopRunAndCooldown(0f);

        Owner.animator.SetFloat("Speed", 0f);
    }

    // ==========================
    // FORCE STOP
    // ==========================
    public void ForceStopAll()
    {
        IsRunning = false;
        ApplyAnimatorRunFlag();

        if (IsStunned)
        {
            IsStunned = false;
            if (_stunIconInstance)
            {
                Object.Destroy(_stunIconInstance);
                _stunIconInstance = null;
            }
        }

        IsRaging = false;
        IsAttacking = false;
    }
}
