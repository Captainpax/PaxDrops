using System;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace PaxDrops
{
    public static class Logger
    {
        private const string Prefix = "[PaxDrops]";
        private const string LogDir = "Mods/PaxDrops/Logs";
        private const string LatestLog = "latest.log";
        private const int MaxLogs = 5;
        private static StreamWriter _writer;
        private static readonly bool EnableDebug = true;

        public static void Init()
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                RotateLogs();

                string fullPath = Path.Combine(LogDir, LatestLog);
                _writer = new StreamWriter(fullPath, append: false) { AutoFlush = true };
                Msg("Logger initialized. Writing to: " + fullPath);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"{Prefix} ❌ Failed to initialize file logger: {ex.Message}");
            }
        }

        public static void Msg(string message)
        {
            MelonLogger.Msg($"{Prefix} {message}");
            WriteToFile($"[INFO] {message}");
        }

        public static void Warn(string message)
        {
            MelonLogger.Warning($"{Prefix} ⚠️ {message}");
            WriteToFile($"[WARN] {message}");
        }

        public static void Error(string message)
        {
            MelonLogger.Error($"{Prefix} ❌ {message}");
            WriteToFile($"[ERROR] {message}");
        }

        public static void LogDebug(string message)
        {
            if (!EnableDebug) return;
            MelonLogger.Msg($"{Prefix} 🛠️ DEBUG: {message}");
            WriteToFile($"[DEBUG] {message}");
        }

        public static void Exception(Exception ex)
        {
            Error($"Exception: {ex.Message}");
            WriteToFile($"[EXCEPTION]\n{ex}");
        }

        public static void UnityLog(string message)
        {
            Debug.Log($"{Prefix} 🪵 {message}");
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
    }
}
