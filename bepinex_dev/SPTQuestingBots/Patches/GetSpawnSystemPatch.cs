using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Bots;
using EFT.Game.Spawning;
using HarmonyLib;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Patches
{
    public class GetSpawnSystemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type baseLocalGameType = Aki.Reflection.Utils.PatchConstants.LocalGameType.BaseType;
            return baseLocalGameType.GetMethod("method_5", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(BotControllerSettings botsSettings, ISpawnSystem spawnSystem, Callback runCallback)
        {
            Type spawnSystemClassType = Aki.Reflection.Utils.PatchConstants.EftTypes.Single((Type x) => x.Name == "SpawnSystemClass");

            LoggingController.LogInfo("Found spawn system type");

            FieldInfo playersCollectionField = AccessTools.Field(spawnSystemClassType, "iplayersCollection_0");
            IPlayersCollection playersCollection = (IPlayersCollection)playersCollectionField.GetValue(spawnSystem);

            LoggingController.LogInfo("Found players: " + playersCollection.Count());

            FieldInfo zonesField = AccessTools.Field(spawnSystemClassType, "ginterface295_0");
            IZones zones = (IZones)zonesField.GetValue(spawnSystem);

            LoggingController.LogInfo("Found zones: " + string.Join(", ", zones.ZoneNames()));

            FieldInfo spawnPointsField = AccessTools.Field(spawnSystemClassType, "ginterface296_0");
            ISpawnPoints spawnPoints = (ISpawnPoints)spawnPointsField.GetValue(spawnSystem);

            LoggingController.LogInfo("Found spawn points: " + spawnPoints.Count);
        }
    }
}
