using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots.Patches.Lighthouse
{
    public class MineDirectionalShouldExplodePatch : ModulePatch
    {
        private static readonly string lighthouseMinesGameObjectName = "Directional_mines_LHZONE";

        protected override MethodBase GetTargetMethod()
        {
            MethodInfo methodInfo = typeof(MineDirectional)
                .GetMethods()
                .First(m => m.IsUnmapped() && m.HasAllParameterTypesInOrder(new Type[] { typeof(Collider) }));

            Controllers.LoggingController.LogInfo("Found method for MineDirectionalShouldExplodePatch: " + methodInfo.Name);

            return methodInfo;
        }

        [PatchPrefix]
        protected static bool PatchPrefix(ref bool __result, MineDirectional __instance, Collider other)
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
                .Where(mine => mine.transform.parent.gameObject.name == lighthouseMinesGameObjectName);

            if (!bridgeMines.Contains(__instance))
            {
                return true;
            }

            if (lightkeeperIslandMonitor.BotsWithQuestsOnLightkeeperIsland.Any(b => b.Id == playerByCollider.Id))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
