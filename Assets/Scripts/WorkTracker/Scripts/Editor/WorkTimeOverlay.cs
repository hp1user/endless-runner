using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

namespace WorkTracker.Editor
{
    [InitializeOnLoad]
    public static class WorkTimeOverlay
    {
        private static string _cachedTimeText = "0h 0m";
        private static double _lastUpdateTime;
        private static GUIStyle _style;
        private static bool _isDragging = false;
        private static Vector2 _dragOffset;

        static WorkTimeOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            _lastUpdateTime = -10000; // Force update on first run
        }

        private static void OnSceneGUI(SceneView view)
        {
            var settings = WorkTrackerSettings.Instance;
            if (settings == null || !settings.ShowWorkTimeOverlay) return;

            double currentTime = EditorApplication.timeSinceStartup;

            // Smart Update Logic
            double timeToNextUpdate = 0;
            
            // Only recalculate text if we passed the scheduled time
            if (currentTime >= _lastUpdateTime)
            {
                // Returns seconds until next minute
                double waitSeconds = UpdateTextAndGetWaitTime();
                _lastUpdateTime = currentTime + waitSeconds;
            }

            Handles.BeginGUI();
            
            if (_style == null)
            {
                _style = new GUIStyle(EditorStyles.label); // changed from helpBox to label for no border
                _style.alignment = TextAnchor.MiddleCenter;
                _style.fontSize = settings.OverlayFontSize;
                _style.fontStyle = FontStyle.Bold;
            }
            _style.fontSize = settings.OverlayFontSize; // Update dynamically

            // Calculate Color based on state
            Color baseColor = Color.green; // Default Work/Active
            if (ActivityMonitor.IsIdle)
            {
                baseColor = Color.red;
            }
            else if (ActivityMonitor.CurrentSessionType == SessionType.View || ActivityMonitor.IsPaused)
            {
                baseColor = Color.yellow;
            }

            // Apply Opacity
            Color displayColor = baseColor;
            displayColor.a = settings.OverlayOpacity;
            _style.normal.textColor = displayColor;

            // Dimension
            float width = 100f;
            float height = 24f;
            
            // Validate position - ensure somewhat on screen
             if (settings.OverlayPosition.x < 0) settings.OverlayPosition.x = 0;
             if (settings.OverlayPosition.y < 0) settings.OverlayPosition.y = 0;

            // Anchor Logic
            // settings.OverlayPosition.x is "Margin from Right"
            float xPos = Screen.width - width - settings.OverlayPosition.x;
            
            // settings.OverlayPosition.y is "Margin from Top" OR "Margin from Bottom" based on anchor
            float yPos = 0;
            if (settings.OverlayAnchor == TextAnchor.LowerRight || settings.OverlayAnchor == TextAnchor.LowerLeft)
            {
                 yPos = Screen.height - height - settings.OverlayPosition.y;
            }
            else
            {
                 yPos = settings.OverlayPosition.y;
            }

            Rect rect = new Rect(xPos, yPos, width, height);

            // Draw Text
            GUI.Label(rect, _cachedTimeText, _style);

            // Handle Dragging
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                _isDragging = true;
                // Capture offset from the mouse to the Top-Left of the rect
                _dragOffset = e.mousePosition - new Vector2(rect.x, rect.y);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _isDragging)
            {
                // Calculate new Top-Left position based on mouse - offset
                Vector2 newRectPos = e.mousePosition - _dragOffset;

                // Convert back to Margins for storage
                // MarginRight = ViewWidth - Width - RectX
                settings.OverlayPosition.x = Screen.width - width - newRectPos.x;
                
                // Determine Vertical Anchor based on position
                if (newRectPos.y > Screen.height / 2)
                {
                    settings.OverlayAnchor = TextAnchor.LowerRight;
                    settings.OverlayPosition.y = Screen.height - height - newRectPos.y;
                }
                else
                {
                    settings.OverlayAnchor = TextAnchor.UpperRight;
                    settings.OverlayPosition.y = newRectPos.y;
                }

                view.Repaint();
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _isDragging)
            {
                _isDragging = false;
                EditorUtility.SetDirty(settings); // Mark dirty
                AssetDatabase.SaveAssets(); // Force save to disk
                e.Use();
            }

            Handles.EndGUI();
        }

        private static double UpdateTextAndGetWaitTime()
        {
            if (SessionManager.CurrentUser == null || SessionManager.CurrentUser.Sessions == null)
            {
                if (_cachedTimeText == "0h 0m") SessionManager.Initialize();
                return 1.0f; // Retry in 1s
            }

            // Calculate total seconds for today
            double totalSeconds = 0;
            DateTime startOfToday = DateTime.Today; // 00:00:00 today

            // Sum up completed sessions from today
            foreach (var session in SessionManager.CurrentUser.Sessions)
            {
                if (session.Date == startOfToday.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
                {
                    if (session.Type == SessionType.Work) 
                    {
                        totalSeconds += session.DurationSeconds;
                    }
                }
            }

            TimeSpan span = TimeSpan.FromSeconds(totalSeconds);
            _cachedTimeText = $"{(int)span.TotalHours}h {span.Minutes}m";
            
            // Smart Wait:
            // If we have 10 seconds, we wait 50 seconds to reach next minute.
            // If we have 60 seconds (0m), we wait 60 seconds.
            // Add slight buffer (0.1s) to ensure we cross the boundary.
            double secondsIntoMinute = totalSeconds % 60.0;
            double waitTime = 60.0 - secondsIntoMinute + 0.1;
            
            // Clamp min wait time just in case (e.g. 0.5s) to avoid spam? No, 0.1 is fine.
            return waitTime;
        }
    }
}
