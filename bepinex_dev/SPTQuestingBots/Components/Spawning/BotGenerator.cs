using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Patches;
using UnityEngine;

namespace SPTQuestingBots.Components.Spawning
{
    public abstract class BotGenerator : MonoBehaviour
    {
        public bool IsSpawningBots { get; private set; } = false;
        public string BotTypeName { get; private set; } = "???";
        public bool HasGeneratedBots { get; private set; } = false;
        public int MaxGeneratedBots { get; private set; } = 0;
        public int GeneratedBotCount { get; private set; } = 0;

        public bool WaitForInitialBossesToSpawn { get; protected set; } = true;
        public bool RespectMaxBotCap { get; protected set; } = true;
        public int MaxAliveBots { get; protected set; } = 10;
        public int MinOtherBotsAllowedToSpawn { get; protected set; } = -99;
        public float RetryTimeSeconds { get; protected set; } = 10;

        protected readonly List<Models.BotSpawnInfo> BotGroups = new List<Models.BotSpawnInfo>();
        private readonly Stopwatch retrySpawnTimer = Stopwatch.StartNew();
        private readonly Stopwatch updateTimer = Stopwatch.StartNew();
        private readonly System.Random random = new System.Random();

        public static int RemainingBotGenerators { get; private set; } = 0;
        public static int CurrentBotGeneratorProgress { get; private set; } = 0;
        public static string CurrentBotGeneratorType { get; private set; } = "???";

        private static Task botGenerationTask = null;
        private static readonly List<Func<Task>> botGeneratorList = new List<Func<Task>>();
        private static readonly Dictionary<Func<BotGenerator>, bool> registeredBotGenerators = new Dictionary<Func<BotGenerator>, bool>();

        public int SpawnedGroupCount => BotGroups.Count(g => g.HaveAllBotsSpawned);
        public int RemainingGroupsToSpawnCount => BotGroups.Count(g => !g.HaveAllBotsSpawned);
        public bool HasRemainingSpawns => !HasGeneratedBots || BotGroups.Any(g => !g.HaveAllBotsSpawned);
        public IReadOnlyCollection<Models.BotSpawnInfo> GetBotGroups() => BotGroups.ToArray();
        public int MaxBotsToGenerate => Math.Min(MaxAliveBots, MaxGeneratedBots - GeneratedBotCount);
        public int GeneratorProgress => 100 * GeneratedBotCount / MaxGeneratedBots;

        public BotGenerator(string _botTypeName)
        {
            BotTypeName = _botTypeName;

            // TODO: This shouldn't be in the constructor; it should be in Awake()
            MaxGeneratedBots = GetMaxGeneratedBots();
            botGeneratorList.Add(generateAllBotsTask(async () => await GenerateBotGroupTask()));
        }

        protected abstract int GetMaxGeneratedBots();
        protected abstract int GetNumberOfBotsAllowedToSpawn();
        protected abstract bool CanSpawnBots();
        protected abstract Task<Models.BotSpawnInfo> GenerateBotGroupTask();
        protected abstract IEnumerable<Vector3> GetSpawnPositionsForBotGroup(Models.BotSpawnInfo botGroup);
        
        protected virtual void Awake()
        {
            
        }

        protected virtual void Update()
        {
            // Reduce the performance impact
            if (updateTimer.ElapsedMilliseconds < 50)
            {
                return;
            }
            updateTimer.Restart();

            // If the previous attempt to spawn a bot failed, wait a minimum amount of time before trying again
            if (retrySpawnTimer.ElapsedMilliseconds < RetryTimeSeconds * 1000)
            {
                return;
            }

            if (!CanSpawnBots() || !AllowedToSpawnBots())
            {
                return;
            }

            StartCoroutine(spawnBotGroups(BotGroups.ToArray()));
        }

        public static void RegisterBotGenerator<T>(bool isPScavGenerator = false) where T : BotGenerator
        {
            registeredBotGenerators.Add(() => Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<T>(), isPScavGenerator);
        }

        public static bool PlayerWantsBotsInRaid()
        {
            RaidSettings raidSettings = Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentRaidSettings;
            if (raidSettings == null)
            {
                return false;
            }

            return raidSettings.BotSettings.BotAmount != EFT.Bots.EBotAmount.NoBots;
        }

        public static bool IsPositionCloseToAnyGeneratedBots(Vector3 position, float distanceFromPlayers, out float distance)
        {
            foreach (BotGenerator botGenerator in Singleton<GameWorld>.Instance.gameObject.GetComponents(typeof(BotGenerator)))
            {
                if (botGenerator == null)
                {
                    continue;
                }

                if (botGenerator.IsPositionCloseToGeneratedBots(position, distanceFromPlayers, out distance))
                {
                    return true;
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public static bool AreAnyPositionsCloseToAnyGeneratedBots(IEnumerable<Vector3> positions, float distanceFromPlayers, out float distance)
        {
            foreach (BotGenerator botGenerator in Singleton<GameWorld>.Instance.gameObject.GetComponents(typeof(BotGenerator)))
            {
                if (botGenerator == null)
                {
                    continue;
                }

                if (botGenerator.AreAnyPositionsCloseToGeneratedBots(positions, distanceFromPlayers, out distance))
                {
                    return true;
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public static IEnumerable<string> GetAllGeneratedBotProfileIDs()
        {
            return GetAllGeneratedBotProfiles().Select(b => b.Id);
        }

        public static IEnumerable<Profile> GetAllGeneratedBotProfiles()
        {
            List<Profile> generatedBotProfiles = new List<Profile>();
            foreach (BotGenerator botGenerator in Singleton<GameWorld>.Instance.gameObject.GetComponents(typeof(BotGenerator)))
            {
                if (botGenerator == null)
                {
                    continue;
                }

                generatedBotProfiles.AddRange(botGenerator.GetGeneratedBotProfiles());
            }

            return generatedBotProfiles;
        }

        public bool AreAnyPositionsCloseToGeneratedBots(IEnumerable<Vector3> positions, float distanceFromPlayers, out float distance)
        {
            foreach (Vector3 position in positions)
            {
                if (IsPositionCloseToGeneratedBots(position, distanceFromPlayers, out distance))
                {
                    return true;
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public bool IsPositionCloseToGeneratedBots(Vector3 position, float distanceFromPlayers, out float distance)
        {
            foreach (Models.BotSpawnInfo botGroup in GetBotGroups())
            {
                IEnumerable<BotOwner> aliveBots = botGroup.SpawnedBots.Where(b => (b != null) && !b.IsDead);
                foreach (BotOwner bot in aliveBots)
                {
                    distance = Vector3.Distance(bot.Position, position);
                    if (distance <= distanceFromPlayers)
                    {
                        return true;
                    }
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public IEnumerable<string> GetGeneratedBotProfileIDs()
        {
            return GetGeneratedBotProfiles().Select(b => b.Id);
        }

        public IEnumerable<Profile> GetGeneratedBotProfiles()
        {
            List<Profile> generatedBotProfiles = new List<Profile>();

            foreach (Models.BotSpawnInfo botGroup in GetBotGroups())
            {
                generatedBotProfiles.AddRange(botGroup.Data.Profiles);
            }

            return generatedBotProfiles;
        }

        public bool TryGetBotGroup(BotOwner bot, out Models.BotSpawnInfo matchingGroupData)
        {
            matchingGroupData = null;

            foreach (Models.BotSpawnInfo info in BotGroups)
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

        public void AddNewBotGroup(Models.BotSpawnInfo newGroup)
        {
            BotGroups.Add(newGroup);
        }

        public static bool TryGetBotGroupFromAnyGenerator(BotOwner bot, out Models.BotSpawnInfo matchingGroupData)
        {
            foreach (BotGenerator botGenerator in Singleton<GameWorld>.Instance.gameObject.GetComponents(typeof(BotGenerator)))
            {
                if (botGenerator?.TryGetBotGroup(bot, out matchingGroupData) == true)
                {
                    return true;
                }
            }

            matchingGroupData = null;
            return false;
        }

        public IReadOnlyCollection<BotOwner> GetSpawnGroupMembers(BotOwner bot)
        {
            IEnumerable<Models.BotSpawnInfo> matchingSpawnGroups = BotGroups.Where(g => g.SpawnedBots.Contains(bot));
            if (matchingSpawnGroups.Count() == 0)
            {
                return new ReadOnlyCollection<BotOwner>(new BotOwner[0]);
            }
            if (matchingSpawnGroups.Count() > 1)
            {
                throw new InvalidOperationException("There is more than one " + BotTypeName + " group with bot " + bot.GetText());
            }

            IEnumerable<BotOwner> botFriends = matchingSpawnGroups.First().SpawnedBots.Where(i => i.Profile.Id != bot.Profile.Id);
            return new ReadOnlyCollection<BotOwner>(botFriends.ToArray());
        }

        public int CountBeforeBotCapIsReached()
        {
            List<Player> allPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList;
            return Singleton<GameWorld>.Instance.GetComponent<LocationData>().MaxTotalBots - allPlayers.Count;
        }

        public bool AllowedToSpawnBots()
        {
            if (!HasGeneratedBots || IsSpawningBots || !HasRemainingSpawns)
            {
                return false;
            }

            if (!CanSpawnAdditionalBots())
            {
                return false;
            }

            if (WaitForInitialBossesToSpawn && !HaveInitialBossWavesSpawned())
            {
                return false;
            }

            // Ensure the raid is progressing before running anything
            float timeSinceSpawning = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetSecondsSinceSpawning();
            if (timeSinceSpawning < 0.01)
            {
                return false;
            }

            return true;
        }

        public bool CanSpawnAdditionalBots()
        {
            // Ensure the total number of bots isn't too close to the bot cap for the map
            if (RespectMaxBotCap && (CountBeforeBotCapIsReached() < MinOtherBotsAllowedToSpawn))
            {
                return false;
            }

            // Don't allow too many alive bots to be on the map for performance and difficulty reasons
            if (BotsAllowedToSpawnForGeneratorType() > 0)
            {
                return true;
            }

            return false;
        }

        public bool HaveInitialBossWavesSpawned()
        {
            if (!PlayerWantsBotsInRaid())
            {
                return true;
            }

            // Factory is too small to wait for bosses or the delay in bot spawns may be noticed
            if (Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Name.ToLower().Contains("factory"))
            {
                return true;
            }

            if (Controllers.BotRegistrationManager.SpawnedBotCount < BotRegistrationManager.ZeroWaveTotalBotCount)
            {
                return false;
            }

            return true;
        }

        public IEnumerable<BotOwner> AliveBots()
        {
            List<BotOwner> aliveBots = new List<BotOwner>();
            foreach (Models.BotSpawnInfo botSpawnInfo in BotGroups)
            {
                aliveBots.AddRange(botSpawnInfo.SpawnedBots.Where(b => (b != null) && !b.IsDead));
            }

            return aliveBots;
        }

        public int BotsAllowedToSpawnForGeneratorType()
        {
            return MaxAliveBots - AliveBots().Count();
        }

        public int RemainingBotsToSpawn()
        {
            int remainingBots = 0;
            foreach (Models.BotSpawnInfo botSpawnInfo in BotGroups)
            {
                remainingBots += botSpawnInfo.RemainingBotsToSpawn;
            }

            return remainingBots;
        }

        public static void RunBotGenerationTasks()
        {
            RaidSettings raidSettings = Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentRaidSettings;

            foreach (Func<BotGenerator> registerBotGenerator in registeredBotGenerators.Keys)
            {
                // Do not enable the bot generator if it's for PScavs and the location does not allow PScavs
                if (registeredBotGenerators[registerBotGenerator] && raidSettings.SelectedLocation.DisabledForScav)
                {
                    continue;
                }

                registerBotGenerator();
            }

            botGenerationTask = runBotGenerationTasks();
        }

        private static async Task runBotGenerationTasks()
        {
            await waitForEFTBotPresetGenerator();

            RemainingBotGenerators = botGeneratorList.Count;

            foreach (Func<Task> botGeneratorCreator in botGeneratorList)
            {
                Task task = botGeneratorCreator();
                await task;

                RemainingBotGenerators--;
            }

            botGeneratorList.Clear();
            RemainingBotGenerators = 0;
        }

        private static async Task waitForEFTBotPresetGenerator()
        {
            while (TryLoadBotsProfilesOnStartPatch.RemainingBotGenerationTasks > 0)
            {
                LoggingController.LogInfo("Waiting for " + TryLoadBotsProfilesOnStartPatch.RemainingBotGenerationTasks + " EFT bot preset generator(s)...");
                await Task.Delay(100);
            }
        }

        private Func<Task> generateAllBotsTask(Func<Task<Models.BotSpawnInfo>> generateBotGroupAction)
        {
            return async () =>
            {
                try
                {
                    CurrentBotGeneratorType = BotTypeName;
                    CurrentBotGeneratorProgress = 0;

                    LoggingController.LogInfo("Generating " + MaxGeneratedBots + " " + BotTypeName + "s...");

                    // Ensure the PMC-conversion chances have remained at 0%
                    ConfigController.AdjustPMCConversionChances(0, true);

                    while (GeneratedBotCount < MaxGeneratedBots)
                    {
                        CurrentBotGeneratorProgress = GeneratorProgress;

                        Models.BotSpawnInfo group = await generateBotGroupAction();

                        BotGroups.Add(group);
                        GeneratedBotCount += group.GeneratedBotCount;
                    }

                    LoggingController.LogInfo("Generating " + MaxGeneratedBots + " " + BotTypeName + "s...done.");
                }
                catch (Exception e)
                {
                    LoggingController.LogError(e.Message);
                    LoggingController.LogError(e.StackTrace);
                }
                finally
                {
                    if (GeneratedBotCount < MaxGeneratedBots)
                    {
                        LoggingController.LogErrorToServerConsole("Only " + GeneratedBotCount + " of " + MaxGeneratedBots + " " + BotTypeName + "s were generated due to an error.");
                    }

                    HasGeneratedBots = true;
                }
            };
        }

        protected async Task<Models.BotSpawnInfo> GenerateBotGroup(WildSpawnType spawnType, BotDifficulty botdifficulty, int bots)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            // In SPT-AKI 3.7.1, this is GClass732
            IBotCreator ibotCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;

            LoggingController.LogInfo("Generating " + botdifficulty.ToString() + " " + BotTypeName + " group (Number of bots: " + bots + ")...");

            Models.BotSpawnInfo botSpawnInfo = null;
            while (botSpawnInfo == null)
            {
                try
                {
                    await Task.Delay(20);

                    EPlayerSide spawnSide = spawnType.GetPlayerSide();
                    GClass652 botProfileData = new GClass652(spawnSide, spawnType, botdifficulty, 0f, null);
                    BotCreationDataClass botSpawnData = await BotCreationDataClass.Create(botProfileData, ibotCreator, bots, botSpawnerClass);

                    botSpawnInfo = new Models.BotSpawnInfo(botSpawnData, this);
                }
                catch (NullReferenceException nre)
                {
                    LoggingController.LogWarning("Generating " + botdifficulty.ToString() + " " + BotTypeName + " group (Number of bots: " + bots + ")...failed. Trying again...");

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

        protected BotDifficulty GetBotDifficulty(EFT.Bots.EBotDifficulty raidDifficulty, double[][] difficultyChances)
        {
            if (raidDifficulty != EFT.Bots.EBotDifficulty.AsOnline)
            {
                return raidDifficulty.ToBotDifficulty();
            }

            return (BotDifficulty)Math.Round(ConfigController.InterpolateForFirstCol(difficultyChances, random.NextDouble()));
        }

        private IEnumerator spawnBotGroups(Models.BotSpawnInfo[] botGroups)
        {
            try
            {
                IsSpawningBots = true;

                // Determine how many PMC's are allowed to spawn
                int allowedSpawns = GetNumberOfBotsAllowedToSpawn();
                List<Models.BotSpawnInfo> botGroupsToSpawn = new List<Models.BotSpawnInfo>();
                for (int i = 0; i < botGroups.Length; i++)
                {
                    if (botGroups[i].HaveAllBotsSpawned)
                    {
                        continue;
                    }

                    // Check if the bot group is allowed to spawn at this time in the raid
                    float raidET = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();
                    if ((raidET < botGroups[i].RaidETRangeToSpawn.Min) || (raidET > botGroups[i].RaidETRangeToSpawn.Max))
                    {
                        continue;
                    }

                    // Ensure there won't be too many bots on the map
                    if (botGroupsToSpawn.Sum(g => g.GeneratedBotCount) + botGroups[i].GeneratedBotCount > allowedSpawns)
                    {
                        break;
                    }

                    botGroupsToSpawn.Add(botGroups[i]);
                }

                if (botGroupsToSpawn.Count == 0)
                {
                    yield break;
                }

                LoggingController.LogInfo("Trying to spawn " + botGroupsToSpawn.Count + " " + BotTypeName + " group(s)...");
                foreach (Models.BotSpawnInfo botGroup in botGroupsToSpawn)
                {
                    yield return spawnBotGroup(botGroup);
                }

                //LoggingController.LogInfo("Trying to spawn " + initialPMCGroupsToSpawn.Count + " initial PMC groups...done.");

            }
            finally
            {
                retrySpawnTimer.Restart();
                IsSpawningBots = false;
            }
        }

        private IEnumerator spawnBotGroup(Models.BotSpawnInfo botGroup)
        {
            if (botGroup.HaveAllBotsSpawned)
            {
                //LoggingController.LogError("PMC group has already spawned.");
                yield break;
            }

            if (!CanSpawnAdditionalBots())
            {
                retrySpawnTimer.Restart();
                LoggingController.LogWarning("Cannot spawn more bots or EFT will not be able to spawn any.");
                yield break;
            }

            Vector3[] spawnPositions = GetSpawnPositionsForBotGroup(botGroup).ToArray();
            if (spawnPositions.Length != botGroup.GeneratedBotCount)
            {
                yield break;
            }

            string spawnPositionText = string.Join(", ", spawnPositions.Select(s => s.ToString()));
            LoggingController.LogInfo("Spawning " + BotTypeName + " group at " + spawnPositionText + "...");

            SpawnBots(botGroup, spawnPositions);

            yield return null;
        }

        private void SpawnBots(Models.BotSpawnInfo botSpawnInfo, Vector3[] positions)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            BotZone closestBotZone = botSpawnerClass.GetClosestZone(positions[0], out float dist);
            foreach (Vector3 position in positions)
            {
                botSpawnInfo.Data.AddPosition(position, GetClosestCorePoint(position).Id);
            }

            IBotCreator ibotCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;

            Action<BotOwner> setBossAction = (bot) => { StartCoroutine(botSpawnInfo.WaitForFollowersAndSetBoss(bot)); };

            ActivateBotMethodsWrapper groupActionsWrapper = new ActivateBotMethodsWrapper(botSpawnerClass, botSpawnInfo, setBossAction);
            Func<BotOwner, BotZone, BotsGroup> getGroupFunction = groupActionsWrapper.GetGroupAndSetEnemies;
            Action<BotOwner> callback = groupActionsWrapper.CreateBotCallback;

            ibotCreator.ActivateBot(botSpawnInfo.Data, closestBotZone, false, getGroupFunction, callback, botSpawnerClass.GetCancelToken());
        }

        private static AICorePoint GetClosestCorePoint(Vector3 position)
        {
            GroupPoint groupPoint = Singleton<IBotGame>.Instance.BotsController.CoversData.GetClosest(position);
            return groupPoint.CorePointInGame;
        }

        internal class ActivateBotMethodsWrapper
        {
            private BotsGroup group = null;
            private BotSpawner botSpawner = null;
            private Models.BotSpawnInfo botSpawnInfo = null;
            private Action<BotOwner> setBossAction = null;
            private Stopwatch stopWatch = new Stopwatch();

            private DeadBodiesController deadBodiesController;
            private FieldInfo botSpawnerAllPlayersFieldInfo;

            public ActivateBotMethodsWrapper(BotSpawner _botSpawnerClass, Models.BotSpawnInfo _botGroup, Action<BotOwner> _setBossAction)
            {
                botSpawner = _botSpawnerClass;
                botSpawnInfo = _botGroup;
                setBossAction = _setBossAction;

                getBotSpawnerFields();
            }

            public BotsGroup GetGroupAndSetEnemies(BotOwner bot, BotZone zone)
            {
                if (group == null)
                {
                    group = createNewGroup(bot, zone);
                }

                botSpawner.method_5(bot);

                return group;
            }

            public void CreateBotCallback(BotOwner bot)
            {
                // I have no idea why BSG passes a stopwatch into this call...
                stopWatch.Start();

                botSpawner.method_11(bot, botSpawnInfo.Data, null, false, stopWatch);

                if (botSpawnInfo.ShouldBotBeBoss(bot))
                {
                    setBossAction(bot);
                }

                LoggingController.LogInfo("Spawned bot " + bot.GetText());
                botSpawnInfo.AddBotOwner(bot);
            }

            private void getBotSpawnerFields()
            {
                FieldInfo botSpawnerDeadBodiesControllerFieldInfo = AccessTools.Field(typeof(BotSpawner), "_deadBodiesController");
                deadBodiesController = (DeadBodiesController)botSpawnerDeadBodiesControllerFieldInfo.GetValue(botSpawner);

                botSpawnerAllPlayersFieldInfo = AccessTools.Field(typeof(BotSpawner), "_allPlayers");
            }

            private BotsGroup createNewGroup(BotOwner initialBot, BotZone zone)
            {
                List<Player> _allPlayers = (List<Player>)botSpawnerAllPlayersFieldInfo.GetValue(botSpawner);

                // --- From BotsGroup.GetGroupAndSetEnemies ---
                EPlayerSide side = initialBot.Profile.Info.Side;

                List<BotOwner> list = new List<BotOwner>();
                foreach (BotOwner botOwner in botSpawner.method_4(initialBot))
                {
                    list.Add(botOwner);
                }

                group = new BotsGroup(zone, botSpawner.BotGame, initialBot, list, deadBodiesController, _allPlayers, true);
                group.TargetMembersCount = botSpawnInfo.GeneratedBotCount;
                botSpawner.Groups.Add(zone, side, group, true);
                // ------------------------------------------

                group.Lock();

                return group;
            }
        }
    }
}
