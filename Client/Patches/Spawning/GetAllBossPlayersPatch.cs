using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;

namespace QuestingBots.Patches.Spawning
{
    public class GetAllBossPlayersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotSpawner).GetMethod(nameof(BotSpawner.GetAllBossPLayers), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(ref List<Player> __result, List<Player> ___AllPlayers)
        {
            __result = ___AllPlayers
                .Where(p => !p.IsAI && (p.AIData?.IAmBoss == true))
                .ToList();

            return false;
        }
    }
}
