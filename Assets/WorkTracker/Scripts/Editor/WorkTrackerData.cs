using System;
using System.Collections.Generic;

namespace WorkTracker
{
    public enum SessionType
    {
        Work,
        View
    }

    [Serializable]
    public class WorkSession
    {
        public string StartTime; // ISO 8601
        public string EndTime;   // ISO 8601
        public double DurationSeconds;
        public string Date;      // YYYY-MM-DD for easier grouping
        public SessionType Type;

        public WorkSession() { }

        public WorkSession(DateTime start, SessionType type)
        {
            StartTime = start.ToString("o");
            Date = start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            DurationSeconds = 0;
            Type = type;
        }

        public void End(DateTime end)
        {
            EndTime = end.ToString("o");
        }
    }

    [Serializable]
    public class UserData
    {
        public string MachineID;
        public string UserName;
        public string Email;
        public string Role = "User"; // User, Admin
        public List<WorkSession> Sessions = new List<WorkSession>();
        
        [NonSerialized] public string SourceFilePath;
    }
}
