using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("3D Sound Settings")]
    public int poolSize = 60;
    public float minDistance = 3f;
    public float maxDistance = 30f;
    public AudioRolloffMode rolloff = AudioRolloffMode.Linear;

    private AudioSource[] pool;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        pool = new AudioSource[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = new GameObject("PooledAudioSource_" + i);
            obj.transform.parent = transform;

            AudioSource src = obj.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;        // FULL 3D
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.rolloffMode = rolloff;

            pool[i] = src;
        }
    }

    public void PlaySound3D(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        if (Instance == null) return;

        foreach (AudioSource src in pool)
        {
            if (!src.isPlaying)
            {
                src.transform.position = position;
                src.clip = clip;
                src.volume = volume;
                src.Play();
                return;
            }
        }
    }

    public void PlaySound3D(AudioClip clip, Vector3 position, float volume, float pitch)
    {
        if (clip == null) return;
        if (Instance == null) return;

        foreach (AudioSource src in pool)
        {
            if (!src.isPlaying)
            {
                src.transform.position = position;
                src.spatialBlend = 1f; // 3D Sound

                src.pitch = pitch;     // ⭐️ PITCH wird jetzt unterstützt
                src.volume = volume;

                src.clip = clip;
                src.Play();

                return;
            }
        }
    }


    public void PlaySound2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        foreach (AudioSource src in pool)
        {
            if (!src.isPlaying)
            {
                src.spatialBlend = 0f; // 2D
                src.volume = volume;
                src.clip = clip;
                src.Play();
                return;
            }
        }
    }
}
