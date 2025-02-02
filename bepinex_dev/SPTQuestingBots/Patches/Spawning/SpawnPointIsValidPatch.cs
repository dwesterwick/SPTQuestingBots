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
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots.Patches.Spawning
{
    public class SpawnPointIsValidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            string methodName = "IsValid";
            Type[] argumentTypes = new Type[] { typeof(ISpawnPoint), typeof(IReadOnlyCollection<IPlayer>), typeof(float), typeof(GClass666) };

            Type targetType = Helpers.TarkovTypeHelpers.FindTargetType(methodName, argumentTypes);
            Controllers.LoggingController.LogInfo("Found type for SpawnPointIsValidPatch: " + targetType.FullName);

            return targetType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                argumentTypes,
                null);
        }

        [PatchPostfix]
        protected static void PatchPostfix(ref bool __result, ISpawnPoint spawnPoint, IReadOnlyCollection<IPlayer> players, float distanceSqr)
        {
            if (!__result)
            {
                return;
            }

            float maxDistanceBetweenSpawnPoints = Singleton<GameWorld>.Instance.gameObject.GetComponent<Components.LocationData>().MaxDistanceBetweenSpawnPoints;
            float exclusionRadius = maxDistanceBetweenSpawnPoints * QuestingBotsPluginConfig.ScavSpawningExclusionRadiusMapFraction.Value;

            float minDistanceFromPlayers = players.HumanAndSimulatedPlayers().Min(p => Vector3.Distance(spawnPoint.Position, p.Position));
            
            if (minDistanceFromPlayers * minDistanceFromPlayers < distanceSqr)
            {
                minDistanceFromPlayers = (float)Math.Sqrt(distanceSqr);
            }

            string message = "Allowed ";
            if (minDistanceFromPlayers  < exclusionRadius)
            {
                message = "Blocked ";
                __result = false;
            }

            message += "spawn that was " + minDistanceFromPlayers + " from players (exclusionRadius=" + Math.Round(exclusionRadius, 1) + ")";
            Controllers.LoggingController.LogInfo(message);
        }
    }
}
