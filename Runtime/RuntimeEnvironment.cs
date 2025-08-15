using System;
using System.Reflection;
using MelonLoader;

namespace PaxDrops.Runtime
{
    /// <summary>
    /// Detects and provides information about the current runtime environment (IL2CPP vs Mono).
    /// This is the central detection system that determines which game assemblies and APIs to use.
    /// </summary>
    public static class RuntimeEnvironment
    {
        private static bool? _isIL2CPP = null;
        private static bool _initialized = false;

        /// <summary>
        /// Gets whether the current runtime is IL2CPP (true) or Mono (false)
        /// </summary>
        public static bool IsIL2CPP
        {
            get
            {
                if (!_initialized)
                {
                    DetectRuntime();
                }
                return _isIL2CPP ?? false;
            }
        }

        /// <summary>
        /// Gets whether the current runtime is Mono (true) or IL2CPP (false)
        /// </summary>
        public static bool IsMono => !IsIL2CPP;

        /// <summary>
        /// Gets the runtime type as a string for logging/debugging
        /// </summary>
        public static string RuntimeType => IsIL2CPP ? "IL2CPP" : "Mono";

        /// <summary>
        /// Detects the current runtime environment using multiple detection methods
        /// </summary>
        private static void DetectRuntime()
        {
            if (_initialized) return;

            try
            {
                // Method 1: Check for Il2CppInterop.Runtime assembly
                var interopCheck = DetectViaIl2CppInterop();
                
                // Method 2: Check for IL2CPP-specific assemblies in loaded assemblies
                var assemblyCheck = DetectViaAssemblyCheck();
                
                // Method 3: Check for IL2CPP-specific types
                var typeCheck = DetectViaTypeCheck();

                // Use majority vote or prioritize Il2CppInterop check
                if (interopCheck.HasValue)
                {
                    _isIL2CPP = interopCheck.Value;
                }
                else if (assemblyCheck.HasValue && typeCheck.HasValue)
                {
                    _isIL2CPP = assemblyCheck.Value || typeCheck.Value;
                }
                else
                {
                    _isIL2CPP = assemblyCheck ?? typeCheck ?? false;
                }

                _initialized = true;
                
                MelonLogger.Msg($"[PaxDrops] [RuntimeEnvironment] ✅ Runtime detected: {RuntimeType}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PaxDrops] [RuntimeEnvironment] ❌ Runtime detection failed: {ex.Message}");
                // Default to Mono if detection fails
                _isIL2CPP = false;
                _initialized = true;
            }
        }

        /// <summary>
        /// Detect runtime by checking for Il2CppInterop.Runtime assembly and ClassInjector
        /// </summary>
        private static bool? DetectViaIl2CppInterop()
        {
            try
            {
                // Check if Il2CppInterop.Runtime assembly is available
                var classInjectorType = Type.GetType(
                    "Il2CppInterop.Runtime.Injection.ClassInjector, Il2CppInterop.Runtime",
                    throwOnError: false);

                if (classInjectorType != null)
                {
                    MelonLogger.Msg("[PaxDrops] [RuntimeEnvironment] 🔍 Il2CppInterop.Runtime detected - IL2CPP runtime confirmed");
                    return true;
                }

                // Additional check for Il2CppInterop types
                var interopType = Type.GetType("Il2CppInterop.Runtime.Il2CppObjectBase, Il2CppInterop.Runtime", throwOnError: false);
                if (interopType != null)
                {
                    MelonLogger.Msg("[PaxDrops] [RuntimeEnvironment] 🔍 Il2CppObjectBase detected - IL2CPP runtime confirmed");
                    return true;
                }

                return null; // Inconclusive
            }
            catch
            {
                return null; // Inconclusive
            }
        }

        /// <summary>
        /// Detect runtime by checking loaded assemblies for IL2CPP-specific patterns
        /// </summary>
        private static bool? DetectViaAssemblyCheck()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                
                foreach (var assembly in assemblies)
                {
                    var name = assembly.GetName().Name;
                    
                    // Check for IL2CPP assembly naming patterns
                    if (name != null && (name.StartsWith("Il2Cpp") || name.Contains("Il2CppInterop")))
                    {
                        MelonLogger.Msg($"[PaxDrops] [RuntimeEnvironment] 🔍 IL2CPP assembly detected: {name}");
                        return true;
                    }
                }

                // Check specifically for our game assemblies
                foreach (var assembly in assemblies)
                {
                    var name = assembly.GetName().Name;
                    if (name == "Assembly-CSharp")
                    {
                        // Check if it contains IL2CPP types
                        try
                        {
                            var consoleType = assembly.GetType("Il2CppScheduleOne.Console");
                            if (consoleType != null)
                            {
                                MelonLogger.Msg("[PaxDrops] [RuntimeEnvironment] 🔍 Il2CppScheduleOne.Console found - IL2CPP runtime");
                                return true;
                            }

                            var monoConsoleType = assembly.GetType("ScheduleOne.Console");
                            if (monoConsoleType != null)
                            {
                                MelonLogger.Msg("[PaxDrops] [RuntimeEnvironment] 🔍 ScheduleOne.Console found - Mono runtime");
                                return false;
                            }
                        }
                        catch
                        {
                            // Continue checking
                        }
                    }
                }

                return null; // Inconclusive
            }
            catch
            {
                return null; // Inconclusive
            }
        }

        /// <summary>
        /// Detect runtime by attempting to load specific types
        /// </summary>
        private static bool? DetectViaTypeCheck()
        {
            try
            {
                // Try to get IL2CPP-specific types first
                var il2cppConsole = Type.GetType("Il2CppScheduleOne.Console, Assembly-CSharp", throwOnError: false);
                if (il2cppConsole != null)
                {
                    MelonLogger.Msg("[PaxDrops] [RuntimeEnvironment] 🔍 Il2CppScheduleOne.Console type found - IL2CPP runtime");
                    return true;
                }

                // Try to get Mono-specific types
                var monoConsole = Type.GetType("ScheduleOne.Console, Assembly-CSharp", throwOnError: false);
                if (monoConsole != null)
                {
                    MelonLogger.Msg("[PaxDrops] [RuntimeEnvironment] 🔍 ScheduleOne.Console type found - Mono runtime");
                    return false;
                }

                return null; // Inconclusive
            }
            catch
            {
                return null; // Inconclusive
            }
        }

        /// <summary>
        /// Force re-detection of runtime (for testing purposes)
        /// </summary>
        public static void ForceRedetection()
        {
            _initialized = false;
            _isIL2CPP = null;
            DetectRuntime();
        }

        /// <summary>
        /// Initialize the runtime detection system
        /// </summary>
        public static void Initialize()
        {
            if (!_initialized)
            {
                DetectRuntime();
            }
        }
    }
}
