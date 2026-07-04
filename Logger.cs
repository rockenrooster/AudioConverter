using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace AudioConverter
{
    /// <summary>
    /// Simple file logger for debugging.
    /// </summary>
    internal static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            GetLogDirectory(),
            "AudioConverter.log"
        );
        private static readonly object _lock = new();

        private static string GetLogDirectory()
        {
            string? processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                string? processDirectory = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrWhiteSpace(processDirectory))
                    return processDirectory;
            }

            return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? ".";
        }

        static Logger()
        {
            // Clear log on startup
            try
            {
                if (File.Exists(LogPath))
                    File.Delete(LogPath);
            }
            catch { }
        }

        public static void Log(string message)
        {
            lock (_lock)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    var logMessage = $"[{timestamp}] [T{threadId}] {message}";
                    File.AppendAllText(LogPath, logMessage + Environment.NewLine);
                    Debug.WriteLine(logMessage);
                }
                catch { }
            }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            Log($"ERROR: {message}");
            if (ex != null)
            {
                Log($"Exception Type: {ex.GetType().Name}");
                Log($"Exception Message: {ex.Message}");
                Log($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Log($"Inner Exception: {ex.InnerException.Message}");
                    Log($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
            }
        }

        public static void LogInfo(string message) => Log($"INFO: {message}");
        public static void LogWarning(string message) => Log($"WARNING: {message}");
        public static void LogDebug(string message) => Log($"DEBUG: {message}");

        public static string GetLogPath() => LogPath;
    }
}
