using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int health = 1;
    public int pointsOnKill = 10;

    protected PlayerHealth player;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHealth>();
    }

    public virtual void TakeDamage(int dmg)
    {
        health -= dmg;
        if (health <= 0) Die();
    }

    protected virtual void Die()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(pointsOnKill);
        Destroy(gameObject);
    }
}
