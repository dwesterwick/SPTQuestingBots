using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches
{
    public class BossLocationSpawnActivatePatch : ModulePatch
    {
        public static int SpawnedWaves { get; set; } = 0;

        protected override MethodBase GetTargetMethod()
        {
            Type localGameType = Aki.Reflection.Utils.PatchConstants.LocalGameType;
            Type targetType = localGameType.GetNestedType("Class1265", BindingFlags.NonPublic | BindingFlags.Instance);

            return targetType.GetMethod("method_1", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(ref BossLocationSpawn bossWave)
        {
            SpawnedWaves++;

            List<Player> allPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList;
            int boxMax = BotGenerator.GetCurrentLocation().BotMax;

            bossWave.IgnoreMaxBots = true;

            string message = "Spawning boss wave ";
            message += SpawnedWaves + "/" + InitBossSpawnLocationPatch.ZeroWaveCount;
            message += " for bot type " + bossWave.BossName;
            message += " (" + (allPlayers.Count - 1) + "/" + boxMax + " total bots on map)";
            message += "...";

            LoggingController.LogInfo(message);
        }
    }
}
