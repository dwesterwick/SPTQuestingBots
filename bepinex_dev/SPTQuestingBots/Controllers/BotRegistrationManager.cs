using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.BotMonitor;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Controllers
{
    public enum BotType
    {
        Undetermined,
        Scav,
        PScav,
        PMC,
        Boss
    }

    public static class BotRegistrationManager
    {
        public static int SpawnedBotCount { get; set; } = 0;
        public static int SpawnedBossCount { get; set; } = 0;
        public static int SpawnedRogueCount { get; set; } = 0;
        public static int SpawnedBossWaves { get; set; } = 0;
        public static int ZeroWaveCount { get; set; } = 0;
        public static int ZeroWaveTotalBotCount { get; set; } = 0;
        public static int ZeroWaveTotalRogueCount { get; set; } = 0;

        private static List<BotOwner> registeredPMCs = new List<BotOwner>();
        private static List<BotOwner> registeredBosses = new List<BotOwner>();
        private static List<BotsGroup> hostileGroups = new List<BotsGroup>();
        private static List<string> sleepingBotIds = new List<string>();

        private static List<WildSpawnType> neverForceHostilityRoles = new List<WildSpawnType>()
        {
            WildSpawnType.bossZryachiy,
            WildSpawnType.followerZryachiy,
            WildSpawnType.gifter,
            WildSpawnType.shooterBTR
        };

        public static IReadOnlyList<BotOwner> PMCs => registeredPMCs.AsReadOnly();
        public static IReadOnlyList<BotOwner> Bosses => registeredBosses.AsReadOnly();
        public static bool IsARegisteredPMC(this BotOwner bot) => registeredPMCs.Contains(bot);
        public static bool IsARegisteredBoss(this BotOwner bot) => registeredBosses.Contains(bot);

        public static void Clear()
        {
            SpawnedBossWaves = 0;
            SpawnedBotCount = 0;
            SpawnedBossCount = 0;
            SpawnedRogueCount = 0;
            ZeroWaveCount = 0;
            ZeroWaveTotalBotCount = 0;
            ZeroWaveTotalRogueCount = 0;

            registeredPMCs.Clear();
            registeredBosses.Clear();
            hostileGroups.Clear();
            sleepingBotIds.Clear();
        }

        public static BotType GetBotType(BotOwner botOwner)
        {
            if (botOwner?.Profile?.Side == null)
            {
                return BotType.Undetermined;
            }

            if (IsBotAPMC(botOwner))
            {
                return BotType.PMC;
            }
            if (IsBotABoss(botOwner))
            {
                return BotType.Boss;
            }
            if (botOwner.Profile.Side == EPlayerSide.Savage)
            {
                if (botOwner.Profile.WillBeAPlayerScav())
                {
                    return BotType.PScav;
                }

                return BotType.Scav;
            }

            return BotType.Undetermined;
        }

        public static float? GetValue(this BotTypeValueConfig botTypeValueConfig, BotType botType)
        {
            switch (botType)
            {
                case BotType.Scav: return botTypeValueConfig.Scav;
                case BotType.PScav: return botTypeValueConfig.PScav;
                case BotType.PMC: return botTypeValueConfig.PMC;
                case BotType.Boss: return botTypeValueConfig.Boss;
            }

            return null;
        }

        public static void WriteMessageForNewBotSpawn(BotOwner botOwner)
        {
            SpawnedBotCount++;
            string message = "Spawned ";

            // If initial PMC's need to spawn but haven't yet, assume the bot is a boss. Otherwise, PMC's should have already spawned. 
            Singleton<GameWorld>.Instance.TryGetComponent(out Components.Spawning.PMCGenerator pmcGenerator);
            if ((pmcGenerator != null) && pmcGenerator.HasGeneratedBots && !pmcGenerator.IsSpawningBots && (pmcGenerator.SpawnedGroupCount == 0))
            {
                message += "boss " + botOwner.GetText() + " (" + registeredBosses.Count + "/" + ZeroWaveTotalBotCount + ")";
            }
            else
            {
                message += "bot #" + SpawnedBotCount + ": " + botOwner.GetText();
            }

            message += " (" + botOwner.Side + ")";
            LoggingController.LogInfo(message);
        }


        public static void RegisterPMC(BotOwner botOwner)
        {
            if (!registeredPMCs.Contains(botOwner))
            {
                registeredPMCs.Add(botOwner);
            }
        }

        public static bool IsBotAPMC(BotOwner botOwner)
        {
            return registeredPMCs.Contains(botOwner);
        }

        public static void RegisterBoss(BotOwner botOwner)
        {
            if (!registeredBosses.Contains(botOwner))
            {
                registeredBosses.Add(botOwner);

                updateAllHostileGroupEnemies();
            }
        }

        public static bool IsBotABoss(BotOwner botOwner)
        {
            return registeredBosses.Contains(botOwner);
        }

        public static void RegisterSleepingBot(BotOwner botOwner)
        {
            if (!sleepingBotIds.Contains(botOwner.ProfileId))
            {
                botOwner
                    .GetOrAddObjectiveManager()
                    .BotMonitor
                    .GetMonitor<BotQuestingDecisionMonitor>()
                    .ForceDecision(BotQuestingDecision.Sleep);

                sleepingBotIds.Add(botOwner.ProfileId);
            }
        }

        public static void UnregisterSleepingBot(BotOwner botOwner)
        {
            if (sleepingBotIds.Contains(botOwner.ProfileId))
            {
                botOwner
                    .GetOrAddObjectiveManager()
                    .BotMonitor
                    .GetMonitor<BotQuestingDecisionMonitor>()
                    .ForceDecision(BotQuestingDecision.None);

                sleepingBotIds.Remove(botOwner.ProfileId);
            }
        }

        public static bool IsBotSleeping(string botId)
        {
            return sleepingBotIds.Contains(botId);
        }

        public static void MakeBotGroupHostileTowardAllBosses(BotOwner bot)
        {
            if (!hostileGroups.Contains(bot.BotsGroup))
            {
                hostileGroups.Add(bot.BotsGroup);

                updateHostileGroupEnemies(bot.BotsGroup);
            }
        }

        private static void updateAllHostileGroupEnemies()
        {
            foreach (BotsGroup hostileGroup in hostileGroups)
            {
                updateHostileGroupEnemies(hostileGroup);
            }
        }

        private static void updateHostileGroupEnemies(BotsGroup group)
        {
            IEnumerable<BotOwner> groupMembers = getAliveGroupMembers(group);
            if (!groupMembers.Any())
            {
                return;
            }

            foreach (BotOwner boss in registeredBosses)
            {
                if ((boss == null) || boss.IsDead)
                {
                    continue;
                }

                if (neverForceHostilityRoles.Contains(boss.Profile.Info.Settings.Role))
                {
                    continue;
                }

                if (group.ContainsEnemy(boss))
                {
                    continue;
                }

                group.AddEnemy(boss, EBotEnemyCause.addPlayer);

                //LoggingController.LogInfo("Group containing " + string.Join(", ", groupMembers.Select(m => m.GetText())) + " is now hostile toward " + boss.GetText());
            }
        }

        private static IEnumerable<BotOwner> getAliveGroupMembers(BotsGroup group)
        {
            List<BotOwner> groupMemberList = new List<BotOwner>();
            for (int m = 0; m < group.MembersCount; m++)
            {
                BotOwner member = group.Member(m);

                if ((member == null) || member.IsDead)
                {
                    continue;
                }

                groupMemberList.Add(member);
            }

            return groupMemberList;
        }
    }
}
