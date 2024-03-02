using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;

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
                if (botOwner.Profile.Nickname.Contains(" ("))
                {
                    return BotType.PScav;
                }

                return BotType.Scav;
            }

            return BotType.Undetermined;
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
                LoggingController.LogInfo(botOwner.GetText() + " is a PMC.");
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

                if (group.ContainsEnemy(boss))
                {
                    continue;
                }

                group.AddEnemy(boss, EBotEnemyCause.addPlayer);

                LoggingController.LogInfo("Group containing " + string.Join(", ", groupMembers.Select(m => m.GetText())) + " is now hostile toward " + boss.GetText());
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
