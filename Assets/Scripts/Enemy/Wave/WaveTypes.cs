using UnityEngine;

public enum EnemyType { Fast, Tank, Ranged }
public enum BossVariant { Boss1, Boss2 }

[System.Serializable]
public struct WeightedEnemy
{
    public EnemyType type;
    [Range(0f, 1f)] public float weight;
}

[System.Serializable]
public struct IntroSpawn
{
    public EnemyType type;
    public int gateIndex;
}

[System.Serializable]
public struct WaveSegment
{
    [Tooltip("How many enemies this segment spawns")]
    public int count;

    [Tooltip("Which gates are allowed in this segment")]
    public int[] activeGates;

    [Tooltip("Random pool used in this segment")]
    public WeightedEnemy[] pool;
}
