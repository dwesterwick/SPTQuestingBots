using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Controllers.Bots.Spawning
{
    public abstract class BotGenerator : MonoBehaviour
    {
        public bool IsDisposed { get; private set; } = false;
        public bool IsSpawningBots { get; protected set; } = false;
        public bool IsGeneratingBots { get; protected set; } = false;
        public string BotTypeName { get; protected set; } = "???";

        internal CoroutineExtensions.EnumeratorWithTimeLimit enumeratorWithTimeLimit = new CoroutineExtensions.EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);

        protected List<Models.BotSpawnInfo> BotGroups = new List<Models.BotSpawnInfo>();

        public int SpawnedGroupCount => BotGroups.Count(g => g.HasSpawned);
        public int RemainingGroupsToSpawnCount => BotGroups.Count(g => !g.HasSpawned);
        public bool HasRemainingSpawns => (BotGroups.Count == 0) || BotGroups.Any(g => !g.HasSpawned);

        public BotGenerator(string _botTypeName)
        {
            BotTypeName = _botTypeName;

            LoggingController.LogInfo("Started " + BotTypeName + " generator");
        }

        public static bool PlayerWantsBotsInRaid()
        {
            RaidSettings raidSettings = Singleton<GameWorld>.Instance.GetComponent<LocationController>().CurrentRaidSettings;
            if (raidSettings == null)
            {
                return false;
            }

            return raidSettings.BotSettings.BotAmount != EFT.Bots.EBotAmount.NoBots;
        }

        public bool TryGetBotGroup(BotOwner bot, out BotSpawnInfo matchingGroupData)
        {
            matchingGroupData = null;

            foreach (BotSpawnInfo info in BotGroups)
            {
                foreach (Profile profile in info.Data.Profiles)
                {
                    if (profile.Id != bot.Profile.Id)
                    {
                        continue;
                    }

                    matchingGroupData = info;
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyCollection<BotOwner> GetSpawnGroupMembers(BotOwner bot)
        {
            IEnumerable<BotSpawnInfo> matchingSpawnGroups = BotGroups.Where(g => g.Owners.Contains(bot));
            if (matchingSpawnGroups.Count() == 0)
            {
                return new ReadOnlyCollection<BotOwner>(new BotOwner[0]);
            }
            if (matchingSpawnGroups.Count() > 1)
            {
                throw new InvalidOperationException("There is more than one " + BotTypeName + " group with bot " + bot.GetText());
            }

            IEnumerable<BotOwner> botFriends = matchingSpawnGroups.First().Owners.Where(i => i.Profile.Id != bot.Profile.Id);
            return new ReadOnlyCollection<BotOwner>(botFriends.ToArray());
        }

        public int NumberOfBotsAllowedToSpawn()
        {
            List<Player> allPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList;
            return Singleton<GameWorld>.Instance.GetComponent<LocationController>().MaxTotalBots - allPlayers.Count;
        }

        public IEnumerable<BotOwner> AliveBots()
        {
            return BotGroups.SelectMany(g => g.Owners.Where(b => (b.BotState == EBotState.Active) && !b.IsDead));
        }

        protected async Task<Models.BotSpawnInfo> GenerateBotGroup(WildSpawnType spawnType, BotDifficulty botdifficulty, int bots)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            // In SPT-AKI 3.7.1, this is GClass732
            IBotCreator ibotCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;

            EPlayerSide spawnSide = BotBrainHelpers.GetSideForWildSpawnType(spawnType);

            LoggingController.LogInfo("Generating " + BotTypeName + " group (Number of bots: " + bots + ")...");

            Models.BotSpawnInfo botSpawnInfo = null;
            while (botSpawnInfo == null)
            {
                try
                {
                    GClass514 botProfileData = new GClass514(spawnSide, spawnType, botdifficulty, 0f, null);
                    GClass513 botSpawnData = await GClass513.Create(botProfileData, ibotCreator, bots, botSpawnerClass);

                    botSpawnInfo = new Models.BotSpawnInfo(botSpawnData);
                }
                catch (NullReferenceException nre)
                {
                    LoggingController.LogWarning("Generating " + BotTypeName + " group (Number of bots: " + bots + ")...failed. Trying again...");

                    LoggingController.LogError(nre.Message);
                    LoggingController.LogError(nre.StackTrace);

                    continue;
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return botSpawnInfo;
        }

        protected void SpawnBots(Models.BotSpawnInfo botSpawnInfo, Vector3[] positions)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            BotZone closestBotZone = botSpawnerClass.GetClosestZone(positions[0], out float dist);
            foreach (Vector3 position in positions)
            {
                botSpawnInfo.Data.AddPosition(position);
            }

            // In SPT-AKI 3.7.1, this is GClass732
            IBotCreator ibotCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;

            GroupActionsWrapper groupActionsWrapper = new GroupActionsWrapper(botSpawnerClass, botSpawnInfo);
            Func<BotOwner, BotZone, BotsGroup> getGroupFunction = new Func<BotOwner, BotZone, BotsGroup>(groupActionsWrapper.GetGroupAndSetEnemies);
            Action<BotOwner> callback = new Action<BotOwner>(groupActionsWrapper.CreateBotCallback);

            LoggingController.LogInfo("Trying to spawn bots...");

            ibotCreator.ActivateBot(botSpawnInfo.Data, closestBotZone, false, getGroupFunction, callback, botSpawnerClass.GetCancelToken());
        }

        internal class GroupActionsWrapper
        {
            private BotsGroup group = null;
            private BotSpawner botSpawnerClass = null;
            private Models.BotSpawnInfo botSpawnInfo = null;
            private Stopwatch stopWatch = new Stopwatch();

            public GroupActionsWrapper(BotSpawner _botSpawnerClass, Models.BotSpawnInfo _botGroup)
            {
                botSpawnerClass = _botSpawnerClass;
                botSpawnInfo = _botGroup;
            }

            public BotsGroup GetGroupAndSetEnemies(BotOwner bot, BotZone zone)
            {
                if (group == null)
                {
                    group = botSpawnerClass.GetGroupAndSetEnemies(bot, zone);
                    group.Lock();
                }

                return group;
            }

            public void CreateBotCallback(BotOwner bot)
            {
                // I have no idea why BSG passes a stopwatch into this call...
                stopWatch.Start();

                MethodInfo method = AccessTools.Method(typeof(BotSpawner), "method_10");
                method.Invoke(botSpawnerClass, new object[] { bot, botSpawnInfo.Data, null, false, stopWatch });

                if (botSpawnInfo.ShouldBotBeBoss(bot))
                {
                    bot.Boss.SetBoss(botSpawnInfo.Count);
                }

                LoggingController.LogInfo("Spawned bot " + bot.GetText());
                botSpawnInfo.Owners.Add(bot);
            }
        }
    }
}
