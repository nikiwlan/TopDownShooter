// BossBeetlePhase0.cs
using UnityEngine;

public sealed class BossBeetlePhase0 : IBossBeetlePhase
{
    private readonly BossBeetleContext _ctx;

    public BossBeetlePhase0(BossBeetleContext ctx) => _ctx = ctx;

    public void Enter(bool triggerRage)
    {
        if (triggerRage)
            _ctx.StartRage();

        _ctx.ApplyAnimatorRunFlag();
    }

    public void Exit() { }

    public void Tick(bool closeForRun)
    {
        // Phase 0: kein Run Start
        int phase = 0;

        Transform origin = _ctx.GetOriginForPhase(phase);
        float attackRange = _ctx.GetAttackRangeForPhase(phase);

        Vector3 playerPoint = _ctx.PlayerTransform.position;
        if (_ctx.Owner.playerBodyCollider != null)
            playerPoint = _ctx.Owner.playerBodyCollider.ClosestPoint(origin.position);

        bool inRange = Vector3.Distance(origin.position, playerPoint) <= attackRange;


        if (!inRange)
        {
            _ctx.MoveTowardsPlayer(_ctx.Owner.walkSpeed, origin);
            _ctx.Owner.animator.SetFloat("Speed", _ctx.Owner.walkSpeed);
        }
        else
        {
            _ctx.Owner.animator.SetFloat("Speed", 0f);
            if (_ctx.CanAttackNow())
                _ctx.TryStartAttack(phase);
        }
    }

    public void OnHeadHit(int damage, Vector3 hitDir, Vector3 hitPoint)
    {
        // Phase 0: HeadHit macht immer Schaden (wie bei dir)
        _ctx.Owner.TakeDamage(damage, hitDir, hitPoint);
    }
}
