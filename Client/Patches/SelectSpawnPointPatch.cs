using EFT;
using EFT.Game.Spawning;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuestingBots.Patches
{
    internal class SelectSpawnPointPatch : ModulePatch
    {
        private static readonly Dictionary<string, ISpawnPoint> spawnPointsForPlayers = new Dictionary<string, ISpawnPoint>();

        protected override MethodBase GetTargetMethod()
        {
            Type targetType = typeof(SpawnSystem);
            string targetMethodName = "SelectSpawnPoint";

            MethodInfo? targetMethod = targetType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name.Contains(targetMethodName));

            if (targetMethod == null)
            {
                throw new InvalidOperationException("Cannot find " + targetMethodName + " in " + targetType.Name);
            }

            return targetMethod;
        }

        [PatchPostfix]
        protected static void PatchPostfix(ISpawnPoint __result, string profileId)
        {
            if (profileId == null)
            {
                return;
            }

            if (spawnPointsForPlayers.ContainsKey(profileId))
            {
                spawnPointsForPlayers[profileId] = __result;
                return;
            }

            spawnPointsForPlayers.Add(profileId, __result);
        }

        public static ISpawnPoint? GetSpawnPoint(IPlayer player) => GetSpawnPoint(player.ProfileId);

        public static ISpawnPoint? GetSpawnPoint(string profileId)
        {
            if (spawnPointsForPlayers.ContainsKey(profileId))
            {
                return spawnPointsForPlayers[profileId];
            }

            return null;
        }
    }
}
