using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.UI.Ragfair;
using SPTQuestingBots.Components.Spawning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Patches
{
    public class GetListByZonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsClass).GetMethod("GetListByZone", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref List<BotOwner> __result, BotZone zone)
        {
            string[] generatedBotIDs = BotGenerator.GetAllGeneratedBotProfileIDs().ToArray();
            List <BotOwner> remainingBots = __result
                .Where(b => !generatedBotIDs.Contains(b.Profile.Id))
                .ToList();

            __result = remainingBots;
        }
    }
}
