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
        private int[] _lastStateHashes;

        private void Start()
        {
            Debug.Log($"<color=white>[AnimTool-System]</color> AnimationEventTrigger <b>Active</b> on {gameObject.name}. Scanning for Animator...", this);
            
            _animator = GetComponent<Animator>();
            if (_animator != null)
            {
                _lastStateHashes = new int[_animator.layerCount];
                Debug.Log($"<color=white>[AnimTool-System]</color> Success: Animator found with <b>{_animator.layerCount}</b> layers.", this);
            }
            else
            {
                Debug.LogError($"<color=red>[AnimTool-Error]</color> CRITICAL: No Animator found on {gameObject.name}! Events will NOT fire.", this);
            }

            if (library == null)
            {
                Debug.LogError($"<color=red>[AnimTool-Error]</color> CRITICAL: Library is missing on {gameObject.name}! No events are defined.", this);
            }
            else
            {
                Debug.Log($"<color=white>[AnimTool-System]</color> Library found: <b>{library.name}</b>. Debug Mode focus: <b>{library.debugMode}</b>", this);
            }
        }

        private void Update()
        {
            // PROOF OF LIFE: This log runs even if logic fails. Check Console!
            if (Time.frameCount % 60 == 0) // Every ~1 second
            {
                bool isSetup = (library != null && _animator != null);
                if (!isSetup) Debug.LogWarning($"[AnimTool-Update] Ticking but Setup incomplete. Lib: {library != null}, Anim: {_animator != null}");
            }

            if (library == null || _animator == null) return;

            // Ensure our hash tracker matches the current layer count
            if (_lastStateHashes == null || _lastStateHashes.Length != _animator.layerCount)
            {
                _lastStateHashes = new int[_animator.layerCount];
            }

            // Buffer for events to fire
            Dictionary<string, (string function, float time, AnimationClip clip, float weight)> winningEvents = new();

            // Iterate through ALL layers
            for (int i = 0; i < _animator.layerCount; i++)
            {
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(i);
                var clipInfo = _animator.GetCurrentAnimatorClipInfo(i);

                // Deep Scan Log
                if (library.debugMode && Time.frameCount % 300 == 0)
                {
                    Debug.Log($"[AnimTool-LayerScan] Layer {i}: Clips Found = {clipInfo.Length}. Playing State = {stateInfo.fullPathHash}");
                }

                if (clipInfo.Length == 0) continue;

                if (stateInfo.fullPathHash != _lastStateHashes[i])
                {
                    _lastStateHashes[i] = stateInfo.fullPathHash;
                }

                float currentNormalizedTime = Mathf.Repeat(stateInfo.normalizedTime, 1f);

                for (int j = 0; j < clipInfo.Length; j++)
                {
                    AnimationClip clip = clipInfo[j].clip;
                    float weight = clipInfo[j].weight;

                    if (weight < 0.05f) continue;

                    if (!_lastNormalizedTimes.TryGetValue(clip, out float lastTime))
                    {
                        if (library.debugMode) Debug.Log($"<color=orange>[AnimTool]</color> Tracking Clip: <b>{clip.name}</b> (Layer {i})", this);
                        lastTime = currentNormalizedTime;
                        _lastNormalizedTimes[clip] = currentNormalizedTime;
                    }

                    var markers = library.GetAllMarkersForClip(clip);
                    
                    // EXTRA LOG: Check if markers are even defined for this clip
                    if (library.debugMode && markers.Count == 0 && Time.frameCount % 600 == 0)
                    {
                        Debug.LogWarning($"[AnimTool-LibraryCheck] No markers defined in Library for active clip: {clip.name}");
                    }

                    foreach (var marker in markers)
                    {
                        if (CheckIfMarkerPassed(lastTime, currentNormalizedTime, marker.time))
                        {
                            if (!winningEvents.ContainsKey(marker.name) || weight > winningEvents[marker.name].weight)
                            {
                                winningEvents[marker.name] = (marker.function, marker.time, clip, weight);
                            }
                        }
                    }
                    _lastNormalizedTimes[clip] = currentNormalizedTime;
                }
            }

            // HEARTBEAT
            if (library.debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"<color=white>[AnimTool-Heartbeat]</color> Monitoring <b>{_animator.layerCount}</b> layers. Tracked Clips: <b>{_lastNormalizedTimes.Count}</b>", this);
            }

            // Fire the "Winning" (highest weight) events across all layers
            foreach (var entry in winningEvents)
            {
                var data = entry.Value;
                TriggerEvent(entry.Key, data.function, data.time, data.clip, data.weight);
            }

            if (_lastNormalizedTimes.Count > 15) CleanUpDictionary();
        }

        private void CleanUpDictionary()
        {
            // Simple cleanup logic: if a clip is no longer being played in any layer, remove it
            var activeClips = new HashSet<AnimationClip>();
            for (int i = 0; i < _animator.layerCount; i++)
            {
                var clips = _animator.GetCurrentAnimatorClipInfo(i);
                foreach (var c in clips) activeClips.Add(c.clip);
            }

            var keysToRemove = new List<AnimationClip>();
            foreach (var key in _lastNormalizedTimes.Keys)
            {
                if (!activeClips.Contains(key)) keysToRemove.Add(key);
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
