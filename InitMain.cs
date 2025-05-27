using MelonLoader;
using PaxDrops.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;
using ScheduleOne;
using System.Collections.Generic;
using System.Reflection;

namespace PaxDrops
{
    public class InitMain : MelonMod
    {
        private bool _commandsRegistered;

        /// <summary>
        /// Called when a scene finishes loading. We hook into the main gameplay scene here.
        /// </summary>
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main") // Replace with your actual main scene name if different
            {
                MelonCoroutines.Start(DelayedInit());
            }
        }

        /// <summary>
        /// Delay initialization to allow ScheduleOne systems to finish loading first.
        /// </summary>
        private System.Collections.IEnumerator DelayedInit()
        {
            yield return new WaitForSeconds(1f); // Delay to ensure console is ready

            if (!_commandsRegistered)
            {
                RegisterConsoleCommands();
                _commandsRegistered = true;
            }

            Logger.Msg("PaxDrops initialized.");
        }

        /// <summary>
        /// Uses reflection to access the internal ScheduleOne.Console.commands dictionary
        /// and inject our custom PaxDropCommand into both the lookup table and visible UI list.
        /// </summary>
        private void RegisterConsoleCommands()
        {
            var consoleType = typeof(Console);
            var commandsField = consoleType.GetField("commands", BindingFlags.NonPublic | BindingFlags.Static);
            var commandsDict = commandsField?.GetValue(null) as Dictionary<string, Console.ConsoleCommand>;

            if (commandsDict != null && !commandsDict.ContainsKey("paxdrop"))
            {
                var command = new PaxDropCommand();

                commandsDict.Add(command.CommandWord, command); // Inject into dispatch system
                Console.Commands.Add(command);                 // Show in console UI list

                Logger.Msg("Registered paxdrop command to ScheduleOne.Console.");
            }
        }
    }
}
