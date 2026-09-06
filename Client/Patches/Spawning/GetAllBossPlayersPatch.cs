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
        protected static bool PatchPrefix(ref List<Player> __result, List<Player> ____allPlayers)
        {
            List<Player> humanBossPlayers = new List<Player>();
            foreach (Player player in ____allPlayers)
            {
                if (player.IsAI)
                {
                    continue;
                }

                if (!player.AIData.IAmBoss)
                {
                    continue;
                }

                humanBossPlayers.Add(player);
            }

            __result = humanBossPlayers;
            return false;
        }
    }
}
