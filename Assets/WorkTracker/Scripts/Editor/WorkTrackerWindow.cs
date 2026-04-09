using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WorkTracker
{
    [InitializeOnLoad]
    public class WorkTrackerWindow : EditorWindow
    {
        private int _selectedTab = 0;
        private string[] _tabs = { "Dashboard", "Calendar", "Users", "Settings" };
        private Vector2 _scrollPos;

        // Auto Open
        static WorkTrackerWindow()
        {
            EditorApplication.delayCall += CheckStartup;
        }

        private static void CheckStartup()
        {
             if (EditorApplication.isPlayingOrWillChangePlaymode) return;
             if (SessionState.GetBool("WorkTrackerStartupShown", false)) return;
             
             SessionManager.Initialize();
             // Always show if not shown yet
             ShowWindow();
             SessionState.SetBool("WorkTrackerStartupShown", true);
        }
        
        // Calendar State
        private DateTime _selectedDate = DateTime.Today;
        private DateTime _currentMonth = DateTime.Today; // For navigation
        private bool _showDayDetail = false;

        // Cache
        private GitHelper.CommitInfo? _cachedLastCommit;
        private List<GitHelper.CommitInfo> _cachedDailyCommits;
        private DateTime _lastDailyCommitsFetchDate;

        // Stats Optimization
        private double _cachedWeekWork = 0;
        private double _cachedTotalWork = 0;
        private int _cachedTotalDays = 0;
        private double _lastStatsUpdateTime = 0;

        [MenuItem("Tools/Work Tracker")]
        public static void ShowWindow()
        {
            WorkTrackerWindow w = GetWindow<WorkTrackerWindow>("Work Tracker");
            w.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            SessionManager.Initialize();
            RefreshGitData();
            _lastStatsUpdateTime = 0; // Force stats update on reload
        }

        private void RefreshGitData()
        {
            _cachedLastCommit = GitHelper.GetLastCommit();
            if (_showDayDetail)
            {
                _cachedDailyCommits = GitHelper.GetCommitsForDate(_selectedDate);
                _lastDailyCommitsFetchDate = _selectedDate;
            }
        }

        private void OnGUI()
        {
            // Modern Header
            DrawHeader();

            GUILayout.Space(10);
            
            // Custom Tab Bar
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            int newTab = GUILayout.Toolbar(_selectedTab, _tabs, GUILayout.Height(30), GUILayout.MinWidth(300));
            if (newTab != _selectedTab)
            {
                _selectedTab = newTab;
                // Refresh git data on tab switch if needed, or just rely on cached
                if (_selectedTab == 0) _cachedLastCommit = GitHelper.GetLastCommit(); 
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(15);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            switch (_selectedTab)
            {
                case 0:
                    DrawDashboard();
                    break;
                case 1:
                    DrawCalendar();
                    break;
                case 2:
                    DrawUsers();
                    break;
                case 3:
                    DrawSettings();
                    break;
            }

            GUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Label("WORK TRACKER", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Sync Data", EditorStyles.toolbarButton))
            {
                SessionManager.SyncData();
                RefreshGitData();
            }
            GUILayout.EndHorizontal();
        }

        private void OnInspectorUpdate()
        {
            if (Time.frameCount % 10 == 0) Repaint();
        }

        private void DrawDashboard()
        {
            UserData user = SessionManager.CurrentUser;
            if (user == null) 
            {
                DrawLoginUI();
                return;
            }

            // --- Status Section ---
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            // User requested removing "Hi"
            GUILayout.Label($"{user.UserName}", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"ID: {(user.MachineID.Length > 6 ? user.MachineID.Substring(0, 6) : "N/A")}...", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Controls
            GUILayout.BeginHorizontal();
            
            // Mode Dropdown styled
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
            SessionType currentType = ActivityMonitor.CurrentSessionType;
            GUILayout.Label("Current Mode:", GUILayout.Width(90));
            SessionType newType = (SessionType)EditorGUILayout.EnumPopup(currentType, GUILayout.Width(100));
            if (newType != currentType) ActivityMonitor.StartSession(newType);
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            // Pause Button
            if (ActivityMonitor.IsPaused)
            {
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // Soft Red
                if (GUILayout.Button("RESUME", GUILayout.Height(25), GUILayout.Width(100)))
                    ActivityMonitor.IsPaused = false;
            }
            else
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); // Soft Green
                if (GUILayout.Button("PAUSE", GUILayout.Height(25), GUILayout.Width(100)))
                    ActivityMonitor.IsPaused = true;
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // --- Last Commit Section ---
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Last Work Committed", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.miniButton)) _cachedLastCommit = GitHelper.GetLastCommit();
            GUILayout.EndHorizontal();

            if (_cachedLastCommit.HasValue && !string.IsNullOrEmpty(_cachedLastCommit.Value.Hash))
            {
                GUILayout.Label($"Message: {_cachedLastCommit.Value.Message}", EditorStyles.wordWrappedLabel);
                GUILayout.Label($"Date: {_cachedLastCommit.Value.Date}", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("No commits found or Git not available.", EditorStyles.miniLabel);
            }
            GUILayout.EndVertical();

            GUILayout.Space(20);

            // --- Stats Section (WORK ONLY) ---
            GUILayout.Label("Work Statistics (Excludes View Time)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            DateTime now = DateTime.Now;
            
            // Optimization: Update Week/Total only every 10 mins (600 seconds)
            if (EditorApplication.timeSinceStartup - _lastStatsUpdateTime > 600.0 || _lastStatsUpdateTime == 0)
            {
                 RecalculateLongTermStats(user, now);
                 _lastStatsUpdateTime = EditorApplication.timeSinceStartup;
            }

            // Optimization: Calculate Today by reverse iteration (assuming chronological append)
            // This is O(N_today) instead of O(N_total)
            double todayWork = 0;
            for (int i = user.Sessions.Count - 1; i >= 0; i--)
            {
                var s = user.Sessions[i];
                // Quick check on date string before parsing if possible, but parsing is safer
                if (DateTime.TryParse(s.Date, out DateTime date))
                {
                     // Check if same day
                     if (date.Year == now.Year && date.Month == now.Month && date.Day == now.Day)
                     {
                         if (s.Type == SessionType.Work) todayWork += s.DurationSeconds;
                     }
                     else
                     {
                         // Found a previous day, since list is chronological, we can stop
                         break;
                     }
                }
            }

            GUILayout.BeginHorizontal();
            DrawStatCard("Today", todayWork, new Color(0.6f, 1f, 0.6f), true);
            DrawStatCard("Week", _cachedWeekWork, new Color(1f, 0.9f, 0.6f), false);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            // Total with Days count
            TimeSpan tTotal = TimeSpan.FromSeconds(_cachedTotalWork);
            string totalStr = $"{(int)tTotal.TotalHours}h {tTotal.Minutes}m / {_cachedTotalDays}d";
            DrawInfoCard("Total", totalStr, new Color(0.6f, 0.8f, 1f));
            
            double avg = _cachedTotalDays > 0 ? _cachedTotalWork / _cachedTotalDays : 0;
            DrawStatCard("Avg/Day", avg, new Color(0.9f, 0.8f, 1f), false);
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
        }
        private bool _showDebug = false;

        private void RecalculateLongTermStats(UserData user, DateTime now)
        {
            _cachedWeekWork = 0;
            _cachedTotalWork = 0;
            _cachedTotalDays = 0;
            DateTime startOfWeek = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
            HashSet<string> workDays = new HashSet<string>();
            
            foreach (var session in user.Sessions)
            {
                if (session.Type == SessionType.Work && DateTime.TryParse(session.Date, out DateTime date))
                {
                    _cachedTotalWork += session.DurationSeconds;
                    if (date.Date >= startOfWeek.Date) _cachedWeekWork += session.DurationSeconds;
                    
                    workDays.Add(session.Date);
                }
            }
            _cachedTotalDays = workDays.Count;
        }

        private void DrawStatCard(string title, double seconds, Color accent, bool showSeconds = false)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            string timeStr = showSeconds 
                ? $"{(int)t.TotalHours}h {t.Minutes}m {t.Seconds}s" 
                : $"{(int)t.TotalHours}h {t.Minutes}m";
            
            DrawInfoCard(title, timeStr, accent);
        }

        private void DrawInfoCard(string title, string content, Color accent)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(80));
            GUILayout.Space(5);
            GUILayout.Label(title, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.fontSize = 24;
            style.alignment = TextAnchor.MiddleCenter;
            
            Color old = GUI.color;
            GUI.color = accent;
            GUILayout.Label(content, style);
            GUI.color = old;
            
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void DrawCalendar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Activity Calendar", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            // Month Navigation
            if (GUILayout.Button("<", EditorStyles.miniButtonLeft, GUILayout.Width(30))) _currentMonth = _currentMonth.AddMonths(-1);
            GUILayout.Label($"{_currentMonth:MMMM yyyy}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100));
            if (GUILayout.Button(">", EditorStyles.miniButtonRight, GUILayout.Width(30))) _currentMonth = _currentMonth.AddMonths(1);
            
            GUILayout.Space(10);
            if (GUILayout.Button("Today", EditorStyles.miniButton)) 
            {
                _selectedDate = DateTime.Today;
                _currentMonth = DateTime.Today;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            DateTime firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            
            // Data aggregation
            Dictionary<int, double> dailyWork = new Dictionary<int, double>();
            Dictionary<int, double> dailyView = new Dictionary<int, double>();
            
            UserData user = SessionManager.CurrentUser;
            if (user != null)
            {
                foreach (var session in user.Sessions)
                {
                    if (DateTime.TryParse(session.Date, out DateTime date))
                    {
                        if (date.Month == _currentMonth.Month && date.Year == _currentMonth.Year)
                        {
                            if (session.Type == SessionType.Work)
                            {
                                if (!dailyWork.ContainsKey(date.Day)) dailyWork[date.Day] = 0;
                                dailyWork[date.Day] += session.DurationSeconds;
                            }
                            else
                            {
                                if (!dailyView.ContainsKey(date.Day)) dailyView[date.Day] = 0;
                                dailyView[date.Day] += session.DurationSeconds;
                            }
                        }
                    }
                }
            }

            // Grid
            int startDayOffset = (int)firstDay.DayOfWeek;
            string[] days = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            
            GUILayout.BeginHorizontal();
            foreach (var d in days) GUILayout.Box(d, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            int currentDay = 1;
            for (int row = 0; row < 6; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 7; col++)
                {
                    if ((row == 0 && col < startDayOffset) || currentDay > daysInMonth)
                    {
                        GUILayout.Label("", GUILayout.Width(50), GUILayout.Height(50));
                    }
                    else
                    {
                        double work = dailyWork.ContainsKey(currentDay) ? dailyWork[currentDay] : 0;
                        double view = dailyView.ContainsKey(currentDay) ? dailyView[currentDay] : 0;
                        double total = work + view;

                        Color bg = Color.white;
                        if (work > 0)
                        {
                            float intensity = Mathf.Clamp01((float)work / 28800f); // 8 hours
                            bg = Color.Lerp(new Color(0.9f, 1f, 0.9f), Color.green, intensity);
                        }
                        
                        GUI.backgroundColor = bg;
                        
                        if (GUILayout.Button($"{currentDay}", GUILayout.Width(50), GUILayout.Height(50)))
                        {
                            _selectedDate = new DateTime(_currentMonth.Year, _currentMonth.Month, currentDay);
                            _showDayDetail = true;
                            // Fetch commits for this new date
                            _cachedDailyCommits = GitHelper.GetCommitsForDate(_selectedDate);
                            _lastDailyCommitsFetchDate = _selectedDate;
                        }
                        GUI.backgroundColor = Color.white;
                        
                        currentDay++;
                    }
                }
                GUILayout.EndHorizontal();
                if (currentDay > daysInMonth) break;
            }

            if (_showDayDetail)
            {
                DrawDayDetail(dailyWork.ContainsKey(_selectedDate.Day) ? dailyWork[_selectedDate.Day] : 0,
                              dailyView.ContainsKey(_selectedDate.Day) ? dailyView[_selectedDate.Day] : 0);
            }
        }

        private void DrawDayDetail(double workSeconds, double viewSeconds)
        {
            GUILayout.Space(20);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Details for {_selectedDate:MMMM dd}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", EditorStyles.miniButton)) _showDayDetail = false;
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            double total = workSeconds + viewSeconds;
            if (total <= 0)
            {
                GUILayout.Label("No activity recorded.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                // Simple Bar Chart
                float width = position.width - 60;
                float workPct = (float)(workSeconds / total);
                float viewPct = (float)(viewSeconds / total);

                Rect r = GUILayoutUtility.GetRect(width, 30);
                
                // Work Bar
                if (workPct > 0)
                {
                    EditorGUI.DrawRect(new Rect(r.x, r.y, r.width * workPct, r.height), Color.green);
                    EditorGUI.LabelField(new Rect(r.x, r.y, r.width * workPct, r.height), $"Work: {(int)(workPct*100)}%", EditorStyles.whiteBoldLabel);
                }
                
                // View Bar
                if (viewPct > 0)
                {
                    EditorGUI.DrawRect(new Rect(r.x + (r.width * workPct), r.y, r.width * viewPct, r.height), Color.blue);
                    EditorGUI.LabelField(new Rect(r.x + (r.width * workPct), r.y, r.width * viewPct, r.height), $"View: {(int)(viewPct*100)}%", EditorStyles.whiteBoldLabel);
                }

                GUILayout.Space(10);
                GUILayout.Label($"Work Time: {TimeSpan.FromSeconds(workSeconds).TotalHours:F1}h", EditorStyles.label);
                GUILayout.Label($"View Time: {TimeSpan.FromSeconds(viewSeconds).TotalHours:F1}h", EditorStyles.label);
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Commits on this day:", EditorStyles.boldLabel);
            
            // Use cached commits
            if (_cachedDailyCommits != null && _cachedDailyCommits.Count > 0)
            {
                foreach (var c in _cachedDailyCommits)
                {
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label(c.Message, EditorStyles.wordWrappedLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(c.Date.ToString("HH:mm"), EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No commits found.", EditorStyles.miniLabel);
            }
            
            GUILayout.EndVertical();
        }

        private string _newUserToCreate = "";

        private void DrawUsers()
        {
            GUILayout.Label("Team Activity", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Create New User:", GUILayout.Width(110));
            _newUserToCreate = EditorGUILayout.TextField(_newUserToCreate);
            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(_newUserToCreate))
                {
                   SessionManager.CreateUser(_newUserToCreate);
                   _newUserToCreate = "";
                   GUI.FocusControl(null); // Clear focus
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (GUILayout.Button("Refresh Data")) { } // Auto-refreshes on load usually

            List<UserData> users = SessionManager.LoadAllUsers();
            foreach (var u in users)
            {
                double work = u.Sessions.Where(s => s.Type == SessionType.Work).Sum(s => s.DurationSeconds);
                double view = u.Sessions.Where(s => s.Type == SessionType.View).Sum(s => s.DurationSeconds);
                
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{u.UserName}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("Delete User", $"Are you sure you want to delete user '{u.UserName}'? This cannot be undone.", "Delete", "Cancel"))
                    {
                        SessionManager.DeleteUser(u);
                        // Force repaint/reload next frame or just continue (the list will refresh on next draw if we trigger it, but modifying list while enumerating is bad)
                        // We are enumerating 'users' which is a local list copy from LoadAllUsers(), so we are safe from modification exception of the source, 
                        // but the UI will show the deleted user until next frame.
                        // Let's force a repaint.
                        GUIUtility.ExitGUI(); 
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Label($"Work: {TimeSpan.FromSeconds(work).TotalHours:F1}h | View: {TimeSpan.FromSeconds(view).TotalHours:F1}h");
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        private void DrawSettings()
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            WorkTrackerSettings settings = WorkTrackerSettings.Instance;
            settings.IdleThresholdSeconds = EditorGUILayout.FloatField("Idle Threshold (s)", settings.IdleThresholdSeconds);
            settings.IgnoreIdle = EditorGUILayout.Toggle("Ignore Idle (Always Track)", settings.IgnoreIdle);
            settings.DayStartHour = EditorGUILayout.IntSlider("Day Start Hour (0-23)", settings.DayStartHour, 0, 23);
            EditorGUILayout.HelpBox($"Sessions starting before {settings.DayStartHour}:00 AM will count as 'Yesterday'. Useful for late-night work.", MessageType.Info);
            settings.SaveIntervalSeconds = EditorGUILayout.FloatField("Save Interval (s)", settings.SaveIntervalSeconds);
            settings.ShowDebugLogs = EditorGUILayout.Toggle("Show Debug Logs", settings.ShowDebugLogs);

            GUILayout.Space(10);
            GUILayout.Label("Overlay Settings", EditorStyles.boldLabel);
            settings.ShowWorkTimeOverlay = EditorGUILayout.Toggle("Show Work Time Overlay", settings.ShowWorkTimeOverlay);
            if (settings.ShowWorkTimeOverlay)
            {
                settings.OverlayUpdateInterval = EditorGUILayout.FloatField("Update Interval (s)", settings.OverlayUpdateInterval);
                settings.OverlayOpacity = EditorGUILayout.Slider("Opacity", settings.OverlayOpacity, 0.1f, 1f);
                settings.OverlayFontSize = EditorGUILayout.IntSlider("Font Size", settings.OverlayFontSize, 8, 24);
                
                EditorGUILayout.BeginHorizontal();
                    settings.OverlayPosition = EditorGUILayout.Vector2Field("Position", settings.OverlayPosition);
                    if (GUILayout.Button("Reset Pos", GUILayout.Width(80))) settings.OverlayPosition = new Vector2(100, 90);
                EditorGUILayout.EndHorizontal();
            }

            // Firebase settings removed as per user request (Local Tracking Only)
            
            GUILayout.Space(15);
            if (GUILayout.Button("Force Save Now")) SessionManager.SaveUserData();
        }

        // --- Login UI (Merged from StartupWindow) ---
        private int _loginUserIndex = 0;
        private string[] _loginUserNames;
        private List<UserData> _loginUsersSorted;
        private string _newUserNameInput = "";

        private void DrawLoginUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(10);
            GUILayout.Label("Welcome to Work Tracker", EditorStyles.largeLabel);
            GUILayout.Space(10);

            if (_loginUsersSorted == null) RefreshLoginUserList();

            GUILayout.Label("Select Existing User", EditorStyles.boldLabel);
            if (_loginUsersSorted != null && _loginUsersSorted.Count > 0)
            {
                _loginUserIndex = EditorGUILayout.Popup(_loginUserIndex, _loginUserNames);
                if (GUILayout.Button("Login Selected User", GUILayout.Height(30)))
                {
                    if (_loginUsersSorted.Count > _loginUserIndex)
                    {
                        SessionManager.SetCurrentUser(_loginUsersSorted[_loginUserIndex]);
                        ActivityMonitor.StartSession(SessionType.View); // Default to View
                    }
                }
            }
            else
            {
                GUILayout.Label("No local users found.", EditorStyles.miniLabel);
            }

            GUILayout.Space(20);
            GUILayout.Label("Create New User", EditorStyles.boldLabel);
            _newUserNameInput = EditorGUILayout.TextField("User Name", _newUserNameInput);
            if (GUILayout.Button("Create & Login", GUILayout.Height(30)))
            {
                if (!string.IsNullOrEmpty(_newUserNameInput))
                {
                    SessionManager.CreateUser(_newUserNameInput);
                    ActivityMonitor.StartSession(SessionType.View);
                    _newUserNameInput = "";
                }
            }

            GUILayout.Space(10);
            GUILayout.EndVertical();
        }

        private void RefreshLoginUserList()
        {
            List<UserData> users = SessionManager.LoadAllUsers();
            _loginUsersSorted = users.OrderByDescending(u => 
            {
                 // Try to find latest session
                 if (u.Sessions != null && u.Sessions.Count > 0) return u.Sessions.Last().Date;
                 return "";
            }).ToList();

            _loginUserNames = new string[_loginUsersSorted.Count];
            for(int i=0; i<_loginUsersSorted.Count; i++)
            {
                 _loginUserNames[i] = _loginUsersSorted[i].UserName;
            }
        }
    }
}
