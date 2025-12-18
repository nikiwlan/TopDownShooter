using UnityEngine;

[CreateAssetMenu(menuName = "Waves/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Info")]
    public string waveName = "Wave";

    [Header("Timing")]
    public float spawnInterval = 1.5f;

    [Header("Intro (scripted, fixed order)")]
    public IntroSpawn[] intro;

    [Header("Segments (random but controlled)")]
    public WaveSegment[] segments;

    [Header("Boss")]
    public bool spawnBoss = false;
    public BossVariant bossVariant = BossVariant.Boss1;
    public int bossGateIndex = 0;
}
