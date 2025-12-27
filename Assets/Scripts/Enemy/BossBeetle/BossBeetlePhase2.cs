// BossBeetlePhase2.cs
using UnityEngine;

public sealed class BossBeetlePhase2 : IBossBeetlePhase
{
    private readonly BossBeetleContext _ctx;

    public BossBeetlePhase2(BossBeetleContext ctx) => _ctx = ctx;

    public void Enter(bool triggerRage)
    {
        if (triggerRage)
            _ctx.StartRage();

        _ctx.ApplyAnimatorRunFlag();
    }

    public void Exit() { }

    public void Tick(bool closeForRun)
    {
        // Phase2:
        // - Während RUN: normales Verhalten wie Phase1 (Attack2)
        // - Während WALK: SpecialHit (Trigger) statt normaler Attack

        float phase2WalkSpeed = _ctx.Owner.walkSpeedPhase2; // <-- NEU

        // 1) Run wie Phase1
        _ctx.TryStartRun(closeForRun);

        if (_ctx.IsRunning)
        {
            _ctx.RunMove(_ctx.Owner.attackOrigin3); // Pivot vorne
            _ctx.Owner.animator.SetFloat("Speed", _ctx.Owner.runSpeed);
            _ctx.ApplyAnimatorRunFlag();
            return;
        }

        // 2) Walk + SpecialHit wenn in Range
        Transform origin = _ctx.Owner.attackOrigin3;  // Special (Phase2)
        float attackRange = _ctx.Owner.attackRange3;

        Vector3 playerPoint = _ctx.PlayerTransform.position;
        if (_ctx.Owner.playerBodyCollider != null)
            playerPoint = _ctx.Owner.playerBodyCollider.ClosestPoint(origin.position);

        bool inRange = Vector3.Distance(origin.position, playerPoint) <= attackRange;

        if (!inRange)
        {
            _ctx.MoveTowardsPlayer(phase2WalkSpeed, origin); // <-- NEU: schneller
            _ctx.Owner.animator.SetFloat("Speed", phase2WalkSpeed); // <-- NEU: schneller
        }
        else
        {
            _ctx.Owner.animator.SetFloat("Speed", 0f);
            _ctx.TryStartSpecialAttack();
        }
    }

    public void OnHeadHit(int damage, Vector3 hitDir, Vector3 hitPoint)
    {
        // wie vorher: nur während RUN verwundbar
        if (_ctx.IsRunning)
            _ctx.Owner.TakeDamage(damage, hitDir, hitPoint);
    }
}
