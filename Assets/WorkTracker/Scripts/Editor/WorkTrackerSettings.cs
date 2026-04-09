using UnityEngine;

namespace WorkTracker
{
    //[CreateAssetMenu(fileName = "WorkTrackerSettings", menuName = "WorkTracker/Settings")]
    public class WorkTrackerSettings : ScriptableObject
    {
        public float IdleThresholdSeconds = 60f; // 1 minute
        public float SaveIntervalSeconds = 300f; // 5 minutes
        public bool ShowDebugLogs = false;
        
        [Header("Overlay Settings")]
        public bool ShowWorkTimeOverlay = true;
        public float OverlayUpdateInterval = 60f; // Default fallback
        public Vector2 OverlayPosition = new Vector2(100, 90);
        public TextAnchor OverlayAnchor = TextAnchor.UpperRight;
        public float OverlayOpacity = 0.5f;
        public int OverlayFontSize = 12;

        [Header("Cloud Sync")]
        public string FirebaseProjectId = "wof-worktracker";
        public string FirebaseApiKey = "AIzaSyC1dBOKLXj7neA1VjIqvRYcO9tYNsLAlF8";

        public bool IgnoreIdle = false; // Enabled idle checking by default
        [Range(0, 23)] public int DayStartHour = 4; // Default to 4 AM for night owls

        private static WorkTrackerSettings _instance;
        public static WorkTrackerSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to load from Resources
                    _instance = Resources.Load<WorkTrackerSettings>("WorkTrackerSettings");
                    
                    // If still null, create a default instance (in memory only if not found)
                    if (_instance == null)
                    {
                        _instance = CreateInstance<WorkTrackerSettings>();
#if UNITY_EDITOR
                        string resPath = "Assets/WorkTracker/Resources";
                        if (!System.IO.Directory.Exists(resPath))
                            System.IO.Directory.CreateDirectory(resPath);
                        
                        UnityEditor.AssetDatabase.CreateAsset(_instance, $"{resPath}/WorkTrackerSettings.asset");
                        UnityEditor.AssetDatabase.SaveAssets();
#endif
                    }
                }
                return _instance;
            }
        }
    }
}
