using System;
using UnityEditor;
using UnityEngine;

namespace WorkTracker
{
    [InitializeOnLoad]
    public static class ActivityMonitor
    {
        private static double _lastActivityTime;
        private static WorkSession _currentSession;
        private static double _lastSaveTime;
        private static double _lastUpdateLoopTime;
        
        public static bool IsPaused { get; set; } = false;
        public static bool IsSessionActive => _currentSession != null;

        public static SessionType CurrentSessionType => _currentSession != null ? _currentSession.Type : SessionType.Work;

        public static bool IsIdle
        {
            get
            {
                if (WorkTrackerSettings.Instance.IgnoreIdle) return false;
                return EditorApplication.timeSinceStartup - _lastActivityTime >= WorkTrackerSettings.Instance.IdleThresholdSeconds;
            }
        }

        static ActivityMonitor()
        {
            EditorApplication.update += OnUpdate;
            EditorApplication.quitting += OnQuit;
            
            // Hook into other events to detect activity
            Selection.selectionChanged += OnActivityDetected;
            EditorApplication.hierarchyChanged += OnActivityDetected;
            EditorApplication.projectChanged += OnActivityDetected;
            
            // Save before domain reload!
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            
            // Undo/Redo hooks for property changes (typing in inspector)
            Undo.postprocessModifications += (m) => { OnActivityDetected(); return m; };
            Undo.undoRedoPerformed += OnActivityDetected;

            // Initialize SessionManager (restores user from SessionState if needed)
            SessionManager.Initialize();
            
            // If we have a user (restored), start tracking immediately so we don't lose time/status on reload
            if (SessionManager.CurrentUser != null)
            {
                SessionType lastType = SessionType.Work; // Could be restored from SessionState if we wanted
                StartSession(lastType); 
            }
            
            _lastActivityTime = EditorApplication.timeSinceStartup;
            _lastUpdateLoopTime = EditorApplication.timeSinceStartup;
        }

        private static void OnBeforeAssemblyReload()
        {
            Save();
        }

        public static void StartSession(SessionType type)
        {
            if (_currentSession != null)
            {
                _currentSession.End(DateTime.Now);
            }
            
            _currentSession = new WorkSession(DateTime.Now, type);
            if (SessionManager.CurrentUser != null)
            {
                SessionManager.CurrentUser.Sessions.Add(_currentSession);
            }
            IsPaused = false;
            _lastActivityTime = EditorApplication.timeSinceStartup;
        }

        private static void OnUpdate()
        {
            UpdateLoop();
        }

        private static void UpdateLoop()
        {
             double currentTime = EditorApplication.timeSinceStartup;
             double dt = currentTime - _lastUpdateLoopTime;
             _lastUpdateLoopTime = currentTime;
             
             if (dt > 60.0) dt = 0; 

             // Check for idle AND pause
             if (!IsPaused && _currentSession != null)
             {
                 // Default to not idle if IgnoreIdle is TRUE. 
                 // If IgnoreIdle is FALSE, check time diff.
                 bool isIdle = !WorkTrackerSettings.Instance.IgnoreIdle && (currentTime - _lastActivityTime >= WorkTrackerSettings.Instance.IdleThresholdSeconds);
                 
                 if (!isIdle)
                 {
                     _currentSession.DurationSeconds += dt;
                     _currentSession.End(DateTime.Now);
                 }
             }
             
             // Auto-save
             if (currentTime - _lastSaveTime > WorkTrackerSettings.Instance.SaveIntervalSeconds)
             {
                 Save();
                 _lastSaveTime = currentTime;
             }
        }

        private static void OnActivityDetected()
        {
            if (!IsPaused)
            {
                _lastActivityTime = EditorApplication.timeSinceStartup;
            }
        }

        private static void OnQuit()
        {
            Save();
        }

        private static void Save()
        {
            if (_currentSession != null)
            {
                _currentSession.End(DateTime.Now);
            }
            SessionManager.SaveUserData();
        }
        
        [InitializeOnLoadMethod]
        static void InitHooks()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (Event.current != null && (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyDown))
            {
                OnActivityDetected();
            }
        }
    }
}
