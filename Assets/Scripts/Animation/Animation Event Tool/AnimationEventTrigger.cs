using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace AnimationTools
{
    /// <summary>
    /// Monitors an Animator and triggers events defined in an AnimationEventLibrary.
    /// This avoids adding events directly to AnimationClips, which can be lost on re-import.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimationEventTrigger : MonoBehaviour
    {
        [Header("Settings")]
        public AnimationEventLibrary library;
        
        [Header("Events")]
        public EventTriggerEvent onEventTriggered;

        [System.Serializable]
        public class EventTriggerEvent : UnityEvent<string> { }

        private Animator _animator;
        private Dictionary<AnimationClip, float> _lastNormalizedTimes = new();
        private int _lastStateHash;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            if (library == null)
            {
                Debug.LogWarning($"[AnimationEventTrigger] Library is missing on {gameObject.name}", this);
            }
        }

        private void Update()
        {
            if (library == null || _animator == null) return;

            // Get current animator state info
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            var clipInfo = _animator.GetCurrentAnimatorClipInfo(0);

            if (clipInfo.Length == 0) return;

            // Handle state changes
            if (stateInfo.fullPathHash != _lastStateHash)
            {
                _lastStateHash = stateInfo.fullPathHash;
                _lastNormalizedTimes.Clear();
            }

            float currentNormalizedTime = Mathf.Repeat(stateInfo.normalizedTime, 1f);

            // HEARTBEAT: Log every 120 frames (~2 seconds) to confirm the script is alive
            if (library.debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"<color=white>[AnimTool-Heartbeat]</color> Monitoring <b>{clipInfo.Length}</b> active clips. Root State: <b>{stateInfo.fullPathHash}</b>", this);
            }

            // Buffer for events to fire - ensures only the highest-weight clip triggers each event name
            Dictionary<string, (string function, float time, AnimationClip clip, float weight)> winningEvents = new();

            // Iterate through ALL active clips in the blend tree
            for (int j = 0; j < clipInfo.Length; j++)
            {
                AnimationClip clip = clipInfo[j].clip;
                float weight = clipInfo[j].weight;

                if (weight < 0.05f) continue;

                if (!_lastNormalizedTimes.TryGetValue(clip, out float lastTime))
                {
                    if (library.debugMode) Debug.Log($"<color=orange>[AnimTool]</color> Tracking new clip: <b>{clip.name}</b>", this);
                    lastTime = currentNormalizedTime;
                    _lastNormalizedTimes[clip] = currentNormalizedTime;
                }

                var markers = library.GetAllMarkersForClip(clip);
                foreach (var marker in markers)
                {
                    if (CheckIfMarkerPassed(lastTime, currentNormalizedTime, marker.time))
                    {
                        // Check if we already have this event buffered from another clip
                        if (!winningEvents.ContainsKey(marker.name) || weight > winningEvents[marker.name].weight)
                        {
                            winningEvents[marker.name] = (marker.function, marker.time, clip, weight);
                        }
                    }
                }
                _lastNormalizedTimes[clip] = currentNormalizedTime;
            }

            // Fire the "Winning" (highest weight) events
            foreach (var entry in winningEvents)
            {
                var data = entry.Value;
                if (library.debugMode)
                {
                    Debug.Log($"<color=cyan>[AnimTool]</color> Triggering <b>{entry.Key}</b> from clip <b>{data.clip.name}</b> (Weight: {data.weight:F2})", this);
                }
                TriggerEvent(entry.Key, data.function, data.time, data.clip, data.weight);
            }

            if (_lastNormalizedTimes.Count > 10) CleanUpDictionary(clipInfo);
        }

        private void CleanUpDictionary(AnimatorClipInfo[] currentClips)
        {
            // Simple cleanup to prevent old clips from staying in memory
            var keysToRemove = new List<AnimationClip>();
            foreach (var key in _lastNormalizedTimes.Keys)
            {
                bool found = false;
                for (int i = 0; i < currentClips.Length; i++)
                {
                    if (currentClips[i].clip == key) { found = true; break; }
                }
                if (!found) keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove) _lastNormalizedTimes.Remove(key);
        }

        private void TriggerEvent(string eventName, string functionName, float time, AnimationClip clip, float weight)
        {
            if (library.debugMode)
            {
                Debug.Log($"<color=yellow>[AnimationEvent]</color> <b>{eventName}</b> marker at <b>{time:F2}s</b> in clip <b>{clip.name}</b> (Weight: <b>{weight:F2}</b>). Calling: <i>{functionName}</i>", this);
            }

            onEventTriggered?.Invoke(eventName);

            if (!string.IsNullOrEmpty(functionName))
            {
                // Create a standard Unity AnimationEvent
                AnimationEvent ae = new AnimationEvent();
                ae.stringParameter = eventName;
                ae.floatParameter = weight; // We pass weight here because animatorClipInfo is engine-managed
                ae.time = time * (clip != null ? clip.length : 1f);
                
                SendMessage(functionName, ae, SendMessageOptions.DontRequireReceiver);
            }
        }

        /// <summary>
        /// Logic to detect if a marker point was crossed during this frame's time step.
        /// Handles wrapping for looping animations.
        /// </summary>
        private bool CheckIfMarkerPassed(float last, float current, float markerTime)
        {
            if (current >= last)
            {
                // Standard progression: Marker is between last and current
                return (last < markerTime && current >= markerTime);
            }
            else
            {
                // Wrapped (looped): Marker is between last and 1.0, OR between 0.0 and current
                return (last < markerTime && markerTime <= 1f) || (markerTime >= 0f && markerTime <= current);
            }
        }
    }
}
