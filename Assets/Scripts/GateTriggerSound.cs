using UnityEngine;

public class GateTriggerSound : MonoBehaviour
{
    [Header("Player Reference (automatisch)")]
    public Transform player;

    [Header("Volume Settings")]
    public float baseVolume = 1f;
    public float maxDistance = 25f;

    [Header("Sounds for Each Object Type")]
    public AudioClip monsterSound;
    public AudioClip sniperSound;
    public AudioClip zombieSound;
    public AudioClip bulletSound;

    private AudioSource gateAudioSource;

    private void Awake()
    {
        gateAudioSource = gameObject.AddComponent<AudioSource>();
        gateAudioSource.spatialBlend = 0f;
        gateAudioSource.playOnAwake = false;
        gateAudioSource.loop = false;
        gateAudioSource.volume = baseVolume;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                Debug.Log($"[GateTriggerSound] Player gefunden: {player.name}");
            }
            else
            {
                Debug.LogWarning("[GateTriggerSound] ⚠ Kein Player gefunden!");
            }
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        float dist = Vector3.Distance(player.position, transform.position);
        float t = Mathf.Clamp01(1f - (dist / maxDistance));

        gateAudioSource.volume = baseVolume * t;
    }

    // Superrobuste Methode: geht bis zu 10 Ebenen hoch
    private EnemyBase FindEnemyBase(Transform t)
    {
        for (int i = 0; i < 10 && t != null; i++)
        {
            EnemyBase e = t.GetComponent<EnemyBase>();
            if (e != null)
            {
                Debug.Log($"[GateTriggerSound] EnemyBase gefunden in Objekt: {t.name}");
                return e;
            }

            t = t.parent;
        }

        Debug.LogWarning("[GateTriggerSound] ❌ EnemyBase NICHT gefunden!");
        return null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        gateAudioSource.PlayOneShot(clip, gateAudioSource.volume);
    }

    private void OnTriggerEnter(Collider other)
    {
        // ENEMY
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"[GateTriggerSound] Enemy erkannt: {other.name}");

            // robust: gehe hoch bis EnemyBase gefunden wird
            EnemyBase enemy = FindEnemyBase(other.transform);
            if (enemy == null)
                return;

            // Typenabfrage
            string typeName = enemy.GetType().Name;
            Debug.Log($"[GateTriggerSound] Enemy-Typ: {typeName}");

            if (enemy is TankEnemy)
                PlaySound(monsterSound);
            else if (enemy is RangedEnemy)
                PlaySound(sniperSound);
            else if (enemy is FastEnemy)
                PlaySound(zombieSound);
        }
    }
}
