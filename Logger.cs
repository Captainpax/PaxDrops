using MelonLoader;

namespace PaxDrops
{
    /// <summary>
    /// Global logging utility for the PaxDrops mod.
    /// Provides shorthand logging methods for standardized output across the project.
    /// </summary>
    public static class Logger
    {
        // Prefix shown in every log entry to identify PaxDrops messages
        private static readonly string PREFIX = "[PaxDrops]";

        /// <summary>
        /// Logs a standard info message to the MelonLoader console.
        /// </summary>
        /// <param name="message">The message to print.</param>
        public static void Msg(string message)
        {
            MelonLogger.Msg($"{PREFIX} {message}");
        }

        /// <summary>
        /// Logs a yellow warning message to the console.
        /// Used for non-breaking issues or suspicious behavior.
        /// </summary>
        /// <param name="message">The warning message to print.</param>
        public static void Warn(string message)
        {
            MelonLogger.Warning($"{PREFIX} ⚠️ {message}");
        }

        /// <summary>
        /// Logs a red error message to the console.
        /// Used for serious or unrecoverable failures.
        /// </summary>
        /// <param name="message">The error message to print.</param>
        public static void Error(string message)
        {
            MelonLogger.Error($"{PREFIX} ❌ {message}");
        }
    }
}