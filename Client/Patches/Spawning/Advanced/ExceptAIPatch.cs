using EFT;
using EFT.Game.Spawning;
using QuestingBots.Helpers;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches.Spawning.Advanced
{
    public class ExceptAIPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayersCollectionExtension).GetMethod(nameof(PlayersCollectionExtension.ExceptAI), BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(ref IEnumerable<IPlayer>  __result, IEnumerable<IPlayer> persons)
        {
            __result = persons.HumanAndSimulatedPlayers();

            return false;
        }
    }
}
