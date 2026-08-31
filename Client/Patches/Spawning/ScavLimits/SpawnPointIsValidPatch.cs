using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using SPT.Reflection.Patching;
using QuestingBots.Helpers;
using UnityEngine;

namespace QuestingBots.Patches.Spawning.ScavLimits
{
    public class SpawnPointIsValidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SpawnPointExtension).GetMethod(
                nameof(SpawnPointExtension.IsValid),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(ISpawnPoint), typeof(IReadOnlyCollection<IPlayer>), typeof(float), typeof(SpawnSystemDebugCollector) },
                null);
        }

        [PatchPostfix]
        protected static void PatchPostfix(ref bool __result, ISpawnPoint spawnPoint, IReadOnlyCollection<IPlayer> players, float distanceSqr)
        {
            if (!__result)
            {
                return;
            }

            if (!QuestingBotsPluginConfig.ScavLimitsEnabled.Value)
            {
                return;
            }

            float maxDistanceBetweenSpawnPoints = Singleton<GameWorld>.Instance.gameObject.GetComponent<Components.LocationData>().MaxDistanceBetweenSpawnPoints;
            float exclusionRadius = maxDistanceBetweenSpawnPoints * QuestingBotsPluginConfig.ScavSpawningExclusionRadiusMapFraction.Value;

            float minDistanceFromPlayers = players.HumanAndSimulatedPlayers().Min(p => Vector3.Distance(spawnPoint.Position, p.Position));

            // In SPT 3.10, distanceSqr is 3m, so this should never happen. However, we should check to be safe.
            if (minDistanceFromPlayers * minDistanceFromPlayers < distanceSqr)
            {
                minDistanceFromPlayers = (float)Math.Sqrt(distanceSqr);
            }

            if (minDistanceFromPlayers < exclusionRadius)
            {
                __result = false;
            }

            /*string message = __result ? "Allowed " : "Blocked ";
            message += "spawn that was " + minDistanceFromPlayers + " from players (exclusionRadius=" + Math.Round(exclusionRadius, 1) + ")";
            Controllers.Singleton<LoggingUtil>.Instance.LogDebug(message);*/
        }
    }
}
