using System.Collections.Generic;
using UnityEngine;

public static class SpawnTracker
{
    // Tracks the last time an object was spawned in a specific lane (keyed by lane index 0, 1, 2)
    private static Dictionary<int, float> lastSpawnTimePerLane = new Dictionary<int, float>();

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
}
