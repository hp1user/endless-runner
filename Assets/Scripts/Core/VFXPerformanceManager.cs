using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages VFX performance, device capability detection, hit VFX rate-limiting,
/// and low-end device optimization (e.g. disabling heavy GPU compute VFX on low-end tablets).
/// </summary>
public static class VFXPerformanceManager
{
    private const string PREF_KEY_VFX_ENABLED = "Setting_VFXEnabled";

    public static bool IsLowEndDevice { get; private set; }
    public static bool IsVFXEnabled { get; set; } = true;

    // Minimum interval between hit VFX spawns per target across distinct frames (in seconds)
    // Limits rapid-fire weapons (e.g. Minigun, AR) from flooding the GPU with dozens of VFX per second
    public const float MIN_HIT_VFX_INTERVAL = 0.08f;

    // Max simultaneous hit VFX allowed within the exact same frame (for shotgun / multi-pellet spread)
    public const int MAX_SAME_FRAME_HITS = 12;

    // Track last spawn time and frame per enemy/boss instance ID
    private static Dictionary<int, float> lastHitVFXTimes = new Dictionary<int, float>();
    private static Dictionary<int, int> lastHitVFXFrames = new Dictionary<int, int>();
    private static Dictionary<int, int> sameFrameHitCounts = new Dictionary<int, int>();

    static VFXPerformanceManager()
    {
        InitializeDeviceProfile();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeDeviceProfile()
    {
        string gpuName = SystemInfo.graphicsDeviceName ?? "";
        int ramMB = SystemInfo.systemMemorySize;

        // Detect low-end mobile hardware:
        // 1. PowerVR GE8320 / Rogue architecture (e.g. Galaxy Tab A7 Lite, Helio P22T)
        // 2. Devices with <= 3200MB RAM
        // 3. Mali-G52 / G31 or low-spec GPUs with poor compute shader performance
        bool isPowerVR = gpuName.IndexOf("PowerVR", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool isLowRam = ramMB > 0 && ramMB <= 3200;
        bool isLowMali = gpuName.IndexOf("Mali-G52", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         gpuName.IndexOf("Mali-G31", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         gpuName.IndexOf("Mali-T", System.StringComparison.OrdinalIgnoreCase) >= 0;

        IsLowEndDevice = isPowerVR || isLowRam || isLowMali;

        if (PlayerPrefs.HasKey(PREF_KEY_VFX_ENABLED))
        {
            IsVFXEnabled = PlayerPrefs.GetInt(PREF_KEY_VFX_ENABLED) == 1;
        }
        else
        {
            // Auto-disable heavy GPU compute VFX on low-end devices by default
            IsVFXEnabled = !IsLowEndDevice;
        }

        Debug.Log($"<color=cyan>[VFXPerformanceManager]</color> GPU: '{gpuName}', RAM: {ramMB}MB. IsLowEndDevice: {IsLowEndDevice}, IsVFXEnabled: {IsVFXEnabled}");
    }

    /// <summary>
    /// Checks whether hit VFX should spawn for the given target, enforcing rate-limiting.
    /// Allows multi-pellet simultaneous blasts (Shotgun / piercing) within the exact same frame up to MAX_SAME_FRAME_HITS,
    /// while throttling continuous rapid-fire weapons across successive frames.
    /// </summary>
    public static bool CanSpawnHitVFX(int targetInstanceId, bool allowSameFrameBurst = true)
    {
        if (!IsVFXEnabled) return false;

        float now = Time.time;
        int currentFrame = Time.frameCount;

        if (allowSameFrameBurst)
        {
            if (lastHitVFXFrames.TryGetValue(targetInstanceId, out int lastFrame) && lastFrame == currentFrame)
            {
                // Multi-pellet blast on the same frame (e.g. Shotgun spread hitting multiple body parts/points)
                int count = sameFrameHitCounts.TryGetValue(targetInstanceId, out int c) ? c : 0;
                if (count < MAX_SAME_FRAME_HITS)
                {
                    sameFrameHitCounts[targetInstanceId] = count + 1;
                    return true;
                }
                return false;
            }
        }

        // New frame hit: check time interval from previous frame hit
        if (lastHitVFXTimes.TryGetValue(targetInstanceId, out float lastTime))
        {
            if (now - lastTime < MIN_HIT_VFX_INTERVAL)
            {
                return false;
            }
        }

        lastHitVFXTimes[targetInstanceId] = now;
        lastHitVFXFrames[targetInstanceId] = currentFrame;
        sameFrameHitCounts[targetInstanceId] = 1;
        return true;
    }

    /// <summary>
    /// Cleans up tracking dictionary when enemies despawn.
    /// </summary>
    public static void ClearTarget(int targetInstanceId)
    {
        lastHitVFXTimes.Remove(targetInstanceId);
        lastHitVFXFrames.Remove(targetInstanceId);
        sameFrameHitCounts.Remove(targetInstanceId);
    }

    /// <summary>
    /// Sets user preference for VFX (0 = disabled / low-end mode, 1 = enabled).
    /// </summary>
    public static void SetVFXEnabled(bool enabled)
    {
        IsVFXEnabled = enabled;
        PlayerPrefs.SetInt(PREF_KEY_VFX_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}

