using System;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace PaxDrops
{
    public enum LogLevel
    {
        Debug = 0,
        Msg = 1,
        Info = 2,
        Warning = 3,
        Error = 4
    }
    public static class Logger
    {
        private const string Prefix = "[PaxDrops]";
        private const string LogDir = "Mods/PaxDrops/Logs";
        private const string LatestLog = "latest.log";
        private const int MaxLogs = 5;

        public static LogLevel MinLogLevel { get; set; } = GetDefaultLogLevel();
        private static StreamWriter? _writer;
        public static string GetBuildConfiguration()
        {
            #if STAGING
            return "STAGING";
            #elif RELEASE
            return "RELEASE";
            #else
            return "DEBUG";
            #endif
        }

        private static LogLevel GetDefaultLogLevel()
        {
            #if STAGING
            return LogLevel.Msg;
            #elif RELEASE
            return LogLevel.Info;
            #else
            return LogLevel.Debug;
            #endif
        }

        public static void SetProductionMode() => MinLogLevel = LogLevel.Info;
        public static void SetStagingMode() => MinLogLevel = LogLevel.Msg;
        public static void SetDebugMode() => MinLogLevel = LogLevel.Debug;

        private static string GetLogPrefix(LogLevel level, string category)
        {
            var levelStr = level switch
            {
                LogLevel.Debug => "DEBUG",
                LogLevel.Msg => "MSG",
                LogLevel.Info => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                _ => "LOG"
            };
            return $"{Prefix} [{category}] [{levelStr}]";
        }


        public static void Init()
        {
            try
            {
                InitializeLogLevel();
                Directory.CreateDirectory(LogDir);
                RotateLogs();

                string fullPath = Path.Combine(LogDir, LatestLog);
                _writer = new StreamWriter(fullPath, append: false) { AutoFlush = true };
                MelonLogger.Msg($"{GetLogPrefix(LogLevel.Msg, "Logger")} Logger initialized. Writing to: " + fullPath);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"{GetLogPrefix(LogLevel.Error, "Logger")} Failed to initialize file logger: {ex.Message}");
            }
        }

        private static void InitializeLogLevel()
        {
            MinLogLevel = GetDefaultLogLevel();
            Info("Logging System initializing...");
            Info($"Build configuration: {GetBuildConfiguration()}");
            Info($"Log level: {MinLogLevel}");
        }

        public static void Msg(string message, string category = "GENERAL LOG")
        {
            if (MinLogLevel > LogLevel.Msg) return;
            MelonLogger.Msg($"{GetLogPrefix(LogLevel.Msg, category)} {message}");
            WriteToFile($"[MSG] {message}");
        }

        public static void Info(string message, string category = "GENERAL LOG")
        {
            if (MinLogLevel > LogLevel.Info) return;
            MelonLogger.Msg($"{GetLogPrefix(LogLevel.Info, category)} {message}");
            WriteToFile($"[INFO] {message}");
        }


        public static void Warn(string message, string category = "GENERAL LOG")
        {
            if (MinLogLevel > LogLevel.Warning) return;
            MelonLogger.Warning($"{GetLogPrefix(LogLevel.Warning, category)} {message}");
            WriteToFile($"[WARN] {message}");
        }

        public static void Error(string message, string category = "GENERAL LOG")
        {
            if (MinLogLevel > LogLevel.Error) return;
            MelonLogger.Error($"{GetLogPrefix(LogLevel.Error, category)} {message}");
            WriteToFile($"[ERROR] {message}");
        }

        public static void Exception(Exception ex, string category = "GENERAL LOG")
        {
            if (MinLogLevel == LogLevel.Debug) return;
            MelonLogger.Error($"{GetLogPrefix(LogLevel.Error, category)} {ex.Message}");
            WriteToFile($"[ERROR] {ex.Message}");
            WriteToFile($"[ERROR] {ex.StackTrace}");
        }

        public static void Debug(string message, string category = "GENERAL LOG")
        {
            if (MinLogLevel > LogLevel.Debug) return;
            MelonLogger.Msg($"{GetLogPrefix(LogLevel.Debug, category)} {message}");
            WriteToFile($"[DEBUG] {message}");
        }

        private static void WriteToFile(string line)
        {
            _writer?.WriteLine($"{DateTime.Now:HH:mm:ss} {line}");
        }

        private static void RotateLogs()
        {
            for (int i = MaxLogs - 1; i > 0; i--)
            {
                string src = Path.Combine(LogDir, i == 1 ? LatestLog : $"log_{i - 1}.txt");
                string dst = Path.Combine(LogDir, $"log_{i}.txt");

                if (File.Exists(src))
                    File.Copy(src, dst, overwrite: true);
            }
        }

        public static void Shutdown()
        {
            _writer?.Close();
            _writer?.Dispose();
            _writer = null;
        }
    }
} 