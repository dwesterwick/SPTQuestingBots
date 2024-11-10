using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace SPTQuestingBots_CustomBotGenExample
{
    public class TestBotGenerator : SPTQuestingBots.Components.Spawning.BotGenerator
    {
        public static int DistanceToSpawnFromMainPlayer { get; } = 5;
        public static int BotsToCache { get; } = 10;

        private SPTQuestingBots.Components.LocationData locationData;
        private bool botSpawnKeyPressed = false;

        public TestBotGenerator() : base("TestBot")
        {
            // This will be used whenever a bot spawns, so let's store a reference to it here instead of searching each time
            locationData = Singleton<GameWorld>.Instance.GetComponent<SPTQuestingBots.Components.LocationData>();

            // This must be significantly reduced from its default value or there will be a lot of time between checks for a keypress
            RetryTimeSeconds = 0.2f;

            // Allow the MaxAliveBots setting to be changed during the raid
            QuestingBotsCustomBotGenExamplePlugin.MaxAliveBots.SettingChanged += setMaxAliveBots;

            setMaxAliveBots(this, new EventArgs());
        }

        private void setMaxAliveBots(object sender, EventArgs e) => MaxAliveBots = QuestingBotsCustomBotGenExamplePlugin.MaxAliveBots.Value;

        protected override void Update()
        {
            if (!QuestingBotsCustomBotGenExamplePlugin.Enabled.Value)
            {
                return;
            }

            // If you (optionally) override Update, you MUST call the base method for the generator to work!
            base.Update();

            if (QuestingBotsCustomBotGenExamplePlugin.SpawnBotKey.Value.IsDown())
            {
                botSpawnKeyPressed = true;
            }
        }

        protected override int GetMaxGeneratedBots() => BotsToCache;
        protected override int GetNumberOfBotsAllowedToSpawn() => 1;

        protected override bool CanSpawnBots()
        {
            if (!didUserRequestABotSpawn())
            {
                return false;
            }

            if (!QuestingBotsCustomBotGenExamplePlugin.Enabled.Value)
            {
                return false;
            }

            if (BotsAllowedToSpawnForGeneratorType() > 0)
            {
                return true;
            }

            return false;
        }

        private bool didUserRequestABotSpawn()
        {
            bool botSpawnRequested = botSpawnKeyPressed;

            // The keypess needs to be cleared each time CanSpawnBots is called or another bot may spawn automatically when one is killed
            botSpawnKeyPressed = false;

            return botSpawnRequested;
        }

        protected override async Task<SPTQuestingBots.Models.BotSpawnInfo> GenerateBotGroupTask()
        {
            return await GenerateBotGroup(WildSpawnType.assault, BotDifficulty.normal, 1);
        }

        protected override IEnumerable<Vector3> GetSpawnPositionsForBotGroup(SPTQuestingBots.Models.BotSpawnInfo botGroup)
        {
            // If Fika is added as a dependency, this could be changed to a client player instead
            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                LoggingController.LogError("Cannot spawn a bot unless the main player is in the raid");
                return Enumerable.Empty<Vector3>();
            }

            Vector3 approxSpawnLocation = mainPlayer.Position + mainPlayer.LookDirection * DistanceToSpawnFromMainPlayer;

            // Bots should be spawned on the NavMesh
            Vector3? exactSpawnLocation = locationData.FindNearestNavMeshPosition(approxSpawnLocation, 1.2f);
            if (!exactSpawnLocation.HasValue)
            {
                LoggingController.LogError($"Cannot find a valid spawn location near {approxSpawnLocation} to spawn a bot");
                return Enumerable.Empty<Vector3>();
            }

            return exactSpawnLocation.Value.ToEnumerable();
        }
    }
}
