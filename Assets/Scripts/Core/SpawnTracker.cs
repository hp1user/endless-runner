using System.Collections.Generic;
using UnityEngine;

public static class SpawnTracker
{
    // Tracks the last time an object was spawned in a specific lane (keyed by lane index 0, 1, 2)
    private static Dictionary<int, float> lastSpawnTimePerLane = new Dictionary<int, float>();

    // Boss safe lane coordination
    private static int currentBossSafeLaneIndex = -1;
    private static float bossSafeLaneExpiryTime = 0f;

    /// <summary>
    /// Checks if it has been long enough since the last spawn in this lane.
    /// </summary>
    public static bool IsLaneSafe(int laneIndex, float safeTimeThreshold = 2.0f)
    {
        if (lastSpawnTimePerLane.TryGetValue(laneIndex, out float lastTime))
        {
            return (Time.time - lastTime) >= safeTimeThreshold;
        }
        // If the lane hasn't been used yet, it's safe
        return true;
    }

    /// <summary>
    /// Records that a spawn just happened in this lane.
    /// </summary>
    public static void RegisterSpawn(int laneIndex)
    {
        lastSpawnTimePerLane[laneIndex] = Time.time;
    }

    /// <summary>
    /// Locks a designated safe lane for boss attack dodging during which obstacle spawners must avoid it.
    /// </summary>
    public static void SetBossSafeLane(int laneIndex, float duration)
    {
        currentBossSafeLaneIndex = laneIndex;
        bossSafeLaneExpiryTime = Time.time + duration;
    }

    /// <summary>
    /// Returns true if this lane is currently designated as the active Boss Safe Lane.
    /// </summary>
    public static bool IsBossSafeLane(int laneIndex)
    {
        if (currentBossSafeLaneIndex == laneIndex && Time.time < bossSafeLaneExpiryTime)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears the boss safe lane lock.
    /// </summary>
    public static void ClearBossSafeLane()
    {
        currentBossSafeLaneIndex = -1;
        bossSafeLaneExpiryTime = 0f;
    }
}
