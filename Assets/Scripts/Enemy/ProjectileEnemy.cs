using UnityEngine;

public class ProjectileEnemy : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
            if (player != null)
                player.TakeDamage(1);
        }
        Destroy(gameObject);
    }
}
