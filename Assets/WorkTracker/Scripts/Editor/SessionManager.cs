using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace WorkTracker
{
    public static class SessionManager
    {
        private static UserData _currentUser;
        private static string _dataFolderPath;

        public static UserData CurrentUser => _currentUser;

        public static void Initialize()
        {
            _dataFolderPath = Path.Combine(Application.dataPath, "WorkTracker/Data");
            if (!Directory.Exists(_dataFolderPath))
            {
                Directory.CreateDirectory(_dataFolderPath);
            }
            
            if (_currentUser == null)
            {
                // Try restore offline
                string lastUser = SessionState.GetString("WorkTracker_CurrentUser", "");
                if (!string.IsNullOrEmpty(lastUser))
                {
                   // We only have the NAME saved in session state. 
                   // LoadLocal using that name as key.
                   // Ideally we should cache the full User object in SessionState or re-load from disk file.
                   LoadLocalUser(lastUser);
                }
            }
        }

        public static void SetCurrentUser(string userName)
        {
             // Overload for OFFLINE / Legacy mode
             LoadLocalUser(userName);
             if (_currentUser == null)
             {
                 _currentUser = new UserData
                 {
                     MachineID = SystemInfo.deviceUniqueIdentifier,
                     UserName = userName,
                     Sessions = new List<WorkSession>()
                 };
                 SaveUserData();
             }
             SessionState.SetString("WorkTracker_CurrentUser", userName);
        }

        public static void SetCurrentUser(UserData user)
        {
            // Explicit set (from Cloud Login)
            _currentUser = user;
            if (_currentUser != null)
            {
                SessionState.SetString("WorkTracker_CurrentUser", user.UserName);
            }
            else
            {
                SessionState.SetString("WorkTracker_CurrentUser", "");
            }
        }

        private static void LoadLocalUser(string userName)
        {
             // Sanitize filename
            string safeName = string.Join("_", userName.Split(Path.GetInvalidFileNameChars()));
            string filePath = GetFilePath(safeName);

            if (File.Exists(filePath))
            {
                LoadUserDataFromFile(filePath);
            }
        }

        public static string GetFilePath(string identifier)
        {
            return Path.Combine(_dataFolderPath, $"User_{identifier}.dat");
        }

        public static void CreateUser(string userName)
        {
             // Local only creation
             SetCurrentUser(userName);
        }

        public static void DeleteUser(UserData user)
        {
             // Delete Local
             if (user != null && !string.IsNullOrEmpty(user.UserName))
             {
                  string safeName = string.Join("_", user.UserName.Split(Path.GetInvalidFileNameChars()));
                  string filePath = GetFilePath(safeName);
                  if (File.Exists(filePath)) File.Delete(filePath);
             }
             
             // Delete Cloud? 
             // We won't implement cloud delete yet (REST DELETE is easy but let's stick to safe path)
        }

        public static List<UserData> LoadAllUsers()
        {
            List<UserData> users = new List<UserData>();
            if (!Directory.Exists(_dataFolderPath)) return users;
            // Local users only
            string[] files = Directory.GetFiles(_dataFolderPath, "User_*.dat");
            foreach (var file in files)
            {
                try
                {
                    string encrypted = File.ReadAllText(file);
                    string json = EncryptionHelper.Decrypt(encrypted);
                    UserData user = JsonUtility.FromJson<UserData>(json);
                    if (user != null)
                    {
                        user.SourceFilePath = file; 
                        users.Add(user);
                    }
                }
                catch { }
            }
            return users;
        }

        public static void SaveUserData()
        {
            SaveUserData(_currentUser);
        }

        public static void SaveUserData(UserData user)
        {
            if (user == null) return;

            // 1. Save Local (Backup/Cache)
            try
            {
                string json = JsonUtility.ToJson(user, true);
                string encrypted = EncryptionHelper.Encrypt(json);
                string safeName = string.Join("_", user.UserName.Split(Path.GetInvalidFileNameChars()));
                string filePath = GetFilePath(safeName);
                File.WriteAllText(filePath, encrypted);
                user.SourceFilePath = filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorkTracker] Local Save Failed: {e.Message}");
            }

            // 2. Save Cloud (Sync)
            // if (FirebaseService.IsLoggedIn) { ... }
        }

        public static void SyncData()
        {
            if (_currentUser == null) return;
            // Force save first
            SaveUserData();
            
            // Push to Git
            GitHelper.CommitAndPush($"Sync data for user {_currentUser.UserName}");
        }

        private static void LoadUserDataFromFile(string filePath)
        {
            try
            {
                string encrypted = File.ReadAllText(filePath);
                string json = EncryptionHelper.Decrypt(encrypted);
                _currentUser = JsonUtility.FromJson<UserData>(json);
                if (_currentUser != null) _currentUser.SourceFilePath = filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorkTracker] Load Failed: {e.Message}");
            }
        }
    }
}
