using UnityEngine;

namespace EndlessRunner.Core
{
    /// <summary>
    /// Provides cross-platform haptic feedback triggers for mobile devices.
    /// Safely vibrates on iOS/Android and logs to console in the editor or unsupported platforms.
    /// </summary>
    public static class HapticFeedbackManager
    {
        public static bool HapticsEnabled = true;

        /// <summary>
        /// Triggers heavy haptic vibration (e.g. boss entrance stomp, massive explosion).
        /// </summary>
        public static void TriggerHeavyImpact()
        {
            if (!HapticsEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#else
            Debug.Log("<color=cyan>[Haptics]</color> Heavy Impact vibration simulated.");
#endif
        }

        /// <summary>
        /// Triggers standard haptic vibration.
        /// </summary>
        public static void TriggerVibration()
        {
            if (!HapticsEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#else
            Debug.Log("<color=cyan>[Haptics]</color> Standard vibration simulated.");
#endif
        }
    }
}

