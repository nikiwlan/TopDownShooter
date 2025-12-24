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
        int phase = 2;

        // Phase2: Priorität 1 = JumpAttack, wenn möglich (neu)
        // (Du kannst die Priorität easy umdrehen, wenn du lieber erst Run willst)
        if (_ctx.CanJumpNow())
        {
            _ctx.TryStartJumpAttack();
            _ctx.Owner.animator.SetFloat("Speed", 0f);
            return;
        }

        // Phase2: ansonsten Run wie Phase1 (shared Mechanik)
        _ctx.TryStartRun(closeForRun);

        if (_ctx.IsRunning)
        {
            _ctx.RunMove();
            _ctx.Owner.animator.SetFloat("Speed", _ctx.Owner.runSpeed);
            _ctx.ApplyAnimatorRunFlag();
            return;
        }

        // Danach walk + (andere) Attack Werte: nutzt deine Phase2 Attack3 Settings
        Transform origin = _ctx.GetOriginForPhase(phase);
        float attackRange = _ctx.GetAttackRangeForPhase(phase);

        bool inRange = Vector3.Distance(origin.position, _ctx.PlayerTransform.position) <= attackRange;

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
        // Vorschlag für Phase2:
        // - verwundbar während Run ODER (wenn du willst) immer verwundbar.
        // Ich setze hier: wie Phase1 -> nur während Run (kannst du 1 Zeile ändern).
        if (_ctx.IsRunning)
            _ctx.Owner.TakeDamage(damage, hitDir, hitPoint);

        // Alternativ: immer
        // _ctx.Owner.TakeDamage(damage, hitDir, hitPoint);
    }
}
