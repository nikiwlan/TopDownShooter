using UnityEngine;

public class BeetleChargeHitbox : MonoBehaviour
{
    [Header("Refs")]
    public BossBeetle boss;

    [Header("Charge Hit")]
    public int damage = 1;
    public float cooldown = 0.6f;

    private float _nextHitTime;

    private void Reset()
    {
        if (!boss) boss = GetComponentInParent<BossBeetle>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!boss) return;
        if (!other.CompareTag("Player")) return;

        // Nur wenn Boss gerade rennt
        if (boss.Ctx == null || !boss.Ctx.IsRunning)
            return;

        // Phase aus Animator holen (Parameter heiﬂt bei dir "Phase")
        if (boss.animator == null) return;
        int phase = boss.animator.GetInteger("Phase");
        if (phase < 1) return; // nur Phase 1 oder 2

        // Cooldown
        if (Time.time < _nextHitTime) return;
        _nextHitTime = Time.time + cooldown;

        boss.ForceStartAttack2();

        // Schaden
        if (boss.player != null)
            boss.player.TakeDamage(damage);
    }
}
