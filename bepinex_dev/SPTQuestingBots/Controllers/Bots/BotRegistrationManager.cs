using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.Controllers.Bots
{
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
                return BotType.Scav;
            }

            return BotType.Undetermined;
        }

        public static void WriteMessageForNewBotSpawn(BotOwner botOwner)
        {
            SpawnedBotCount++;
            string message = "Spawned ";

            // If initial PMC's need to spawn but haven't yet, assume the bot is a boss. Otherwise, PMC's should have already spawned. 
            if (BotGenerator.CanSpawnPMCs && (BotGenerator.SpawnedInitialPMCCount == 0) && !BotGenerator.IsSpawningPMCs)
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
            }
        }

        public static bool IsBotABoss(BotOwner botOwner)
        {
            return registeredBosses.Contains(botOwner);
        }
    }
}
