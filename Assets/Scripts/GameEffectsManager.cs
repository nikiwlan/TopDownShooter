// GameEffectsManager.cs
using UnityEngine;

public static class GameEffectsManager
{
    private static float timeSlowEndTime = 0f;
    private static float timeSlowFactor = 1f;

    public static bool TimeSlowActive => Time.time < timeSlowEndTime;
    public static float Factor => timeSlowFactor;
    public static float Remaining => Mathf.Max(0f, timeSlowEndTime - Time.time);

    public static void ActivateTimeSlow(float duration, float factor)
    {
        timeSlowFactor = factor;
        // verlängern, falls bereits aktiv
        timeSlowEndTime = Mathf.Max(timeSlowEndTime, Time.time + duration);
        Debug.Log($"[GameEffects] TimeSlow aktiviert: {duration:0.##}s @ x{factor}");
    }
}
