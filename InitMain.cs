using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using PaxDrops.Commands;
using S1API.GameTime;

namespace PaxDrops
{
    public class InitMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Logger.Msg(">> PaxDrops loaded. Waiting for scene 'Main'...");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main") return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            Logger.Msg(">> Scene 'Main' detected. Starting PaxDrops systems...");

            TierLevel.Init();
            DataBase.Init();
            MrStacks.Init();
            DeadDrop.Init();

            TimeManager.OnDayPass += DeadDrop.HandleDayPass;

            RegisterConsoleCommands();
        }

        private void RegisterConsoleCommands()
        {
            var paxCommands = new List<ScheduleOne.Console.ConsoleCommand>
            {
                new PaxGiveMoneyCommand(),
                new PaxSpawnDropCommand(),
                new PaxSetTimeCommand(),
                new PaxTeleportCommand()
            };

            var console = ScheduleOne.Console.Instance;

            var dictField = typeof(ScheduleOne.Console).GetField("commands", BindingFlags.NonPublic | BindingFlags.Static);
            var commandDict = dictField?.GetValue(null) as Dictionary<string, ScheduleOne.Console.ConsoleCommand>;

            if (commandDict == null)
            {
                Debug.LogError("❌ PaxDrops: Could not access Console.commands dictionary.");
                return;
            }

            foreach (var cmd in paxCommands)
            {
                if (!commandDict.ContainsKey(cmd.CommandWord))
                    commandDict.Add(cmd.CommandWord, cmd);

                if (!ScheduleOne.Console.Commands.Contains(cmd))
                    ScheduleOne.Console.Commands.Add(cmd);
            }

            Debug.Log("✅ PaxDrops console commands registered.");
        }
    }
}
