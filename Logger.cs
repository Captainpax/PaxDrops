using System;
using MelonLoader;
using UnityEngine;

namespace PaxDrops
{
    /// <summary>
    /// Global logging utility for the PaxDrops mod.
    /// Provides standardized, timestamped log output and utility helpers for warnings, errors, and debug messages.
    /// </summary>
    public static class Logger
    {
        // Static prefix tag for all logs
        private const string Prefix = "[PaxDrops]";
        private const string TimeFormat = "HH:mm:ss";

        // Toggle this to enable or disable debug logs
        private static readonly bool EnableDebug = true;

        /// <summary>
        /// Logs a standard info message to the MelonLoader console.
        /// </summary>
        public static void Msg(string message)
        {
            MelonLogger.Msg($"{Timestamp()} {Prefix} {message}");
        }

        /// <summary>
        /// Logs a warning message to the console.
        /// </summary>
        public static void Warn(string message)
        {
            MelonLogger.Warning($"{Timestamp()} {Prefix} ⚠️ {message}");
        }

        /// <summary>
        /// Logs an error message to the console.
        /// </summary>
        public static void Error(string message)
        {
            MelonLogger.Error($"{Timestamp()} {Prefix} ❌ {message}");
        }

        /// <summary>
        /// Logs a debug message if debug logging is enabled.
        /// </summary>
        public static void LogDebug(string message)
        {
            if (!EnableDebug) return;
            MelonLogger.Msg($"{Timestamp()} {Prefix} 🛠️ DEBUG: {message}");
        }

        /// <summary>
        /// Logs an exception with stack trace.
        /// </summary>
        public static void Exception(Exception ex)
        {
            MelonLogger.Error($"{Timestamp()} {Prefix} ❌ Exception: {ex.Message}");
            MelonLogger.Error($"{Prefix} StackTrace:\n{ex.StackTrace}");
        }

        /// <summary>
        /// Optionally logs directly to Unity’s in-game console. Use for HUD debugging.
        /// </summary>
        public static void UnityLog(string message)
        {
            Debug.Log($"{Timestamp()} {Prefix} 🪵 {message}");
        }

        /// <summary>
        /// Returns a formatted timestamp.
        /// </summary>
        private static string Timestamp()
        {
            return $"[{DateTime.Now.ToString(TimeFormat)}]";
        }
    }
}
