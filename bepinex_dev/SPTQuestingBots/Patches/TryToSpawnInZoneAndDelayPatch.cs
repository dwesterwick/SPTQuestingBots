using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using EFT.Game.Spawning;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class TryToSpawnInZoneAndDelayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotSpawner).GetMethod("TryToSpawnInZoneAndDelay", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(BotZone botZone, GClass591 data, bool withCheckMinMax, bool newWave, List<ISpawnPoint> pointsToSpawn, bool forcedSpawn)
        {
            if (!QuestingBotsPluginConfig.ShowSpawnDebugMessages.Value)
            {
                return;
            }

            IEnumerable<string> botData = data.Profiles.Select(p => "[" + p.Info.Settings.Role.ToString() + " " + p.Nickname + "]");
            LoggingController.LogInfo("Trying to spawn wave with: " + string.Join(", ", botData) + "...");

            //System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            //LoggingController.LogInfo(stackTrace.ToString());
        }
    }
}
