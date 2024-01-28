using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.UI.Ragfair;
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
            List<string> generatedBotIDs = new List<string>();
            foreach (Components.Spawning.BotGenerator botGenerator in Singleton<GameWorld>.Instance.gameObject.GetComponents(typeof(Components.Spawning.BotGenerator)))
            {
                if (botGenerator == null)
                {
                    continue;
                }

                foreach (Models.BotSpawnInfo botGroup in botGenerator.GetBotGroups())
                {
                    generatedBotIDs.AddRange(botGroup.SpawnedBots.Select(b => b.Profile.Id));
                }
            }

            List<BotOwner> remainingBots = __result
                .Where(b => !generatedBotIDs.Contains(b.Profile.Id))
                .ToList();

            __result = remainingBots;
        }
    }
}
