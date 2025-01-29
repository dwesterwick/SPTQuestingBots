using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Game.Spawning;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SPTQuestingBots.Patches.Spawning
{
    public class SpawnPointIsValidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3300).GetMethod(
                "IsValid",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(ISpawnPoint), typeof(IReadOnlyCollection<IPlayer>), typeof(float), typeof(GClass666) },
                null);
        }

        [PatchPostfix]
        protected static void PatchPostfix(bool __result, ISpawnPoint spawnPoint, IReadOnlyCollection<IPlayer> players, float distanceSqr)
        {
            if (!__result)
            {
                return;
            }

            float minDistance = players.Select(p => Vector3.Distance(spawnPoint.Position, p.Position)).Min();

            Controllers.LoggingController.LogInfo("Allowed spawn that was " + minDistance + " from players (min=" + Math.Sqrt(distanceSqr) + ")");
        }
    }
}
