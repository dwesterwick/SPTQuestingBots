using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Patches
{
    public class MineDirectionalShouldExplodePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MineDirectional).GetMethod("method_1", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref bool __result, MineDirectional __instance, Collider other)
        {
            if (!Singleton<GameWorld>.Instance.TryGetComponent(out Components.LightkeeperIslandMonitor lightkeeperIslandMonitor))
            {
                return true;
            }

            Player playerByCollider = Singleton<GameWorld>.Instance.GetPlayerByCollider(other);
            if (playerByCollider == null)
            {
                return true;
            }

            IEnumerable<MineDirectional> bridgeMines = Singleton<GameWorld>.Instance.MineManager.Mines
                .Where(mine => mine.transform.parent.gameObject.name == "Directional_mines_LHZONE");

            if (!bridgeMines.Contains(__instance))
            {
                return true;
            }

            if (lightkeeperIslandMonitor.BotsWithQuestsOnLightkeeperIsland.Any(b => b.Id == playerByCollider.Id))
            {
                LoggingController.LogWarning("Disabline mine for " + playerByCollider.GetText());

                __result = true;
                return false;
            }

            return true;
        }
    }
}
