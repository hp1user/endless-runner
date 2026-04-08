using UnityEngine;
using System.Collections.Generic;

namespace AnimationTools
{
    [System.Serializable]
    public class ClipMarkerData
    {
        public AnimationClip clip;
        [Tooltip("Markers in normalized time (0.0 to 1.0)")]
        public List<float> markers = new List<float>();
        [HideInInspector] public bool isExpanded = true;
    }

    [System.Serializable]
    public class AnimationEventDefinition
    {
        public string eventName;
        public string functionName;
        public List<ClipMarkerData> clipData = new List<ClipMarkerData>();
    }

    [CreateAssetMenu(fileName = "AnimationEventLibrary", menuName = "Animation/Event Library")]
    public class AnimationEventLibrary : ScriptableObject
    {
        public bool debugMode = false;
        public List<AnimationEventDefinition> events = new List<AnimationEventDefinition>();

        // We use a simple list for storage, but we can provide helper methods for the runtime logic.
        
        public IEnumerable<float> GetMarkersForClip(AnimationClip clip, string eventName)
        {
            foreach (var evt in events)
            {
                if (evt.eventName == eventName)
                {
                    foreach (var cd in evt.clipData)
                    {
                        if (cd.clip == clip) return cd.markers;
                    }
                }
            }
            return null;
        }

        public List<(string name, string function, float time)> GetAllMarkersForClip(AnimationClip clip)
        {
            var result = new List<(string, string, float)>();
            if (clip == null) return result;

            foreach (var evt in events)
            {
                foreach (var cd in evt.clipData)
                {
                    if (cd.clip == clip)
                    {
                        foreach (var m in cd.markers)
                        {
                            result.Add((evt.eventName, evt.functionName, m));
                        }
                    }
                }
            }
            return result;
        }
    }
}
