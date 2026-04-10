using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using Comfort.Common;
using QuestingBots.Utils;

namespace QuestingBots.Patches.Spawning.Advanced
{
    public class ExceptAIPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            string methodName = "ExceptAI";

            Type targetType = Helpers.TarkovTypeHelpers.FindTargetTypeByMethod(methodName);
            Singleton<LoggingUtil>.Instance.LogInfo("Found type for ExceptAIPatch: " + targetType.FullName);

            return targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(ref IEnumerable<IPlayer>  __result, IEnumerable<IPlayer> persons)
        {
            __result = persons.HumanAndSimulatedPlayers();

            return false;
        }
    }
}
