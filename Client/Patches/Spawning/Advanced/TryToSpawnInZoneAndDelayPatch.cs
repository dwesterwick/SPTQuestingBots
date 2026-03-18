using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;
using EFT.Game.Spawning;
using QuestingBots.Controllers;
using Comfort.Common;
using QuestingBots.Utils;

namespace QuestingBots.Patches.Spawning.Advanced
{
    public class TryToSpawnInZoneAndDelayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotSpawner).GetMethod(nameof(BotSpawner.TryToSpawnInZoneAndDelay), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotZone botZone, BotCreationDataClass data, bool withCheckMinMax, bool newWave, List<ISpawnPoint> pointsToSpawn, bool forcedSpawn)
        {
            if (!QuestingBotsPluginConfig.ShowSpawnDebugMessages.Value)
            {
                return;
            }

            IEnumerable<string> botData = data.Profiles.Select(p => "[" + p.Info.Settings.Role.ToString() + " " + p.Nickname + "]");
            Singleton<LoggingUtil>.Instance.LogInfo("Trying to spawn wave with: " + string.Join(", ", botData) + "...");
        }
    }
}
