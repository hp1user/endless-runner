using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace WorkTracker
{
    public static class GitHelper
    {
        public struct CommitInfo
        {
            public string Hash;
            public string Message;
            public string Author;
            public DateTime Date;
        }

        public static CommitInfo GetLastCommit()
        {
            string output = RunGitCommand("log -1 --pretty=format:\"%h|%s|%an|%ad\" --date=iso");
            return ParseCommit(output);
        }

        public static List<CommitInfo> GetCommitsForDate(DateTime date)
        {
            // Git log for a specific date range (00:00 to 23:59)
            string start = date.ToString("yyyy-MM-dd 00:00:00");
            string end = date.ToString("yyyy-MM-dd 23:59:59");
            
            string output = RunGitCommand($"log --since=\"{start}\" --until=\"{end}\" --pretty=format:\"%h|%s|%an|%ad\" --date=iso");
            
            List<CommitInfo> commits = new List<CommitInfo>();
            if (string.IsNullOrEmpty(output)) return commits;

            string[] lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                commits.Add(ParseCommit(line));
            }
            return commits;
        }

        private static CommitInfo ParseCommit(string line)
        {
            if (string.IsNullOrEmpty(line)) return new CommitInfo();

            string[] parts = line.Split('|');
            if (parts.Length >= 4)
            {
                DateTime.TryParse(parts[3], out DateTime dt);
                return new CommitInfo
                {
                    Hash = parts[0],
                    Message = parts[1],
                    Author = parts[2],
                    Date = dt
                };
            }
            return new CommitInfo { Message = line };
        }

        private static string RunGitCommand(string args)
        {
            string gitPath = ResolveGitPath();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gitPath, args)
                {
                    WorkingDirectory = Application.dataPath, 
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Adjust working directory to project root
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                startInfo.WorkingDirectory = projectRoot;

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode != 0)
                    {
                         UnityEngine.Debug.LogWarning($"[WorkTracker] Git Error: {error}");
                    }

                    return output.Trim();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[WorkTracker] Git command failed: {e.Message}");
                return "";
            }
        }

        public static void CommitAndPush(string message)
        {
            // Sync Data folder
            string dataPath = "Assets/WorkTracker/Data";
            RunGitCommand($"add \"{dataPath}\"");
            RunGitCommand($"commit -m \"[WorkTracker] {message}\"");
            
            // Push
            // Note: This relies on the system having cached credentials or SSH keys setup.
            // If it hangs, we might need to handle it, but for now we assume transparent auth.
            RunGitCommand("push");
        }

        private static string ResolveGitPath()
        {
            // Windows default
            if (Application.platform == RuntimePlatform.WindowsEditor) return "git";

            // Mac/Linux: Check common paths
            string[] commonPaths = { 
                "/usr/bin/git", 
                "/usr/local/bin/git", 
                "/opt/homebrew/bin/git",
                "/bin/git"
            };
            
            foreach (var path in commonPaths)
            {
                if (File.Exists(path)) return path;
            }

            // Fallback: Use 'which git' to find it?
            try 
            {
                ProcessStartInfo psi = new ProcessStartInfo("which", "git")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process p = Process.Start(psi))
                {
                    string res = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit();
                    if (!string.IsNullOrEmpty(res) && File.Exists(res)) return res;
                }
            }
            catch {}

            return "git"; // Final fallback to PATH
        }
    }
}
