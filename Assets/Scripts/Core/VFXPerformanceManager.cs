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

    // Minimum interval between hit VFX spawns per target (in seconds)
    // Limits rapid-fire weapons (e.g. Minigun, AR) from flooding the GPU with dozens of VFX per second
    public const float MIN_HIT_VFX_INTERVAL = 0.10f;

    // Track last spawn time per enemy/boss instance ID
    private static Dictionary<int, float> lastHitVFXTimes = new Dictionary<int, float>();

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
    /// </summary>
    public static bool CanSpawnHitVFX(int targetInstanceId)
    {
        if (!IsVFXEnabled) return false;

        float now = Time.time;
        if (lastHitVFXTimes.TryGetValue(targetInstanceId, out float lastTime))
        {
            if (now - lastTime < MIN_HIT_VFX_INTERVAL)
            {
                return false;
            }
        }

        lastHitVFXTimes[targetInstanceId] = now;
        return true;
    }

    /// <summary>
    /// Cleans up tracking dictionary when enemies despawn.
    /// </summary>
    public static void ClearTarget(int targetInstanceId)
    {
        if (lastHitVFXTimes.ContainsKey(targetInstanceId))
        {
            lastHitVFXTimes.Remove(targetInstanceId);
        }
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

