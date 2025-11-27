using System.Collections.Generic;
using UnityEngine;

public class EnemyShootManager : MonoBehaviour
{
    public static EnemyShootManager Instance;

    [Header("Group Shooting Sound")]
    public AudioClip groupShootSound;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Optional Pitch Variation")]
    public bool randomizePitch = true;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    private List<Vector3> pendingShots = new List<Vector3>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterShot(Vector3 pos)
    {
        pendingShots.Add(pos);
    }

    void LateUpdate()
    {
        if (pendingShots.Count == 0)
            return;

        if (groupShootSound != null)
        {
            float pitch = 1f;
            if (randomizePitch)
                pitch = Random.Range(minPitch, maxPitch);

            // Spiele EINEN Sound an der Position des lautesten Schusses
            Vector3 pos = pendingShots[0];

            AudioManager.Instance.PlaySound3D(groupShootSound, pos, volume, pitch);
        }

        pendingShots.Clear();
    }
}
