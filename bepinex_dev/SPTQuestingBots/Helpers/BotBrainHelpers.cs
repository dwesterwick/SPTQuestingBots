using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SPTQuestingBots.Helpers
{
    public static class BotBrainHelpers
    {
        public static void AddQuestingBotsBrainLayers(Configuration.BrainLayerPrioritiesConfig brainLayerPriorities)
        {
            IEnumerable<BotBrainType> allNonSniperBrains = GetAllNonSniperBrains();
            IEnumerable<BotBrainType> allBrains = allNonSniperBrains.AddAllSniperBrains();

            LoggingController.LogDebug("Loading QuestingBots...changing bot brains for sleeping: " + string.Join(", ", allBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Sleep.SleepingLayer), allBrains.ToStringList(), brainLayerPriorities.Sleeping);

            if (!ConfigController.Config.Questing.Enabled)
            {
                return;
            }

            LoggingController.LogDebug("Loading QuestingBots...changing bot brains for questing: " + string.Join(", ", allNonSniperBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Objective.BotObjectiveLayer), allNonSniperBrains.ToStringList(), brainLayerPriorities.Questing);

            LoggingController.LogDebug("Loading QuestingBots...changing bot brains for following: " + string.Join(", ", allBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Follow.BotFollowerLayer), allBrains.ToStringList(), brainLayerPriorities.Following);
            BrainManager.AddCustomLayer(typeof(BotLogic.Follow.BotFollowerRegroupLayer), allBrains.ToStringList(), brainLayerPriorities.Regrouping);
        }

        public static IEnumerable<BotBrainType> AddTestBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("BossTest", WildSpawnType.bossTest) });
        }

        public static IEnumerable<BotBrainType> AddBTRBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("BTR", WildSpawnType.shooterBTR) });
        }

        public static IEnumerable<BotBrainType> AddNormalScavBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[]
            {
                new BotBrainType("Assault", WildSpawnType.assault),
                new BotBrainType("CursAssault", WildSpawnType.cursedAssault)
            });
        }

        public static IEnumerable<BotBrainType> AddSniperScavBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Marksman", WildSpawnType.marksman) });
        }

        public static IEnumerable<BotBrainType> AddBloodhoundBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("ArenaFighter", WildSpawnType.arenaFighter) });
        }

        public static IEnumerable<BotBrainType> AddCrazyScavBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Obdolbs", WildSpawnType.arenaFighterEvent) });
        }

        public static IEnumerable<BotBrainType> AddSantaBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Gifter", WildSpawnType.gifter) });
        }

        public static IEnumerable<BotBrainType> AddRogueBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("ExUsec", WildSpawnType.exUsec) });
        }

        public static IEnumerable<BotBrainType> AddRaiderBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("PMC", WildSpawnType.pmcBot) });
        }

        public static IEnumerable<BotBrainType> AddPMCBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[]
            {
                new BotBrainType("PmcBear", WildSpawnType.pmcBEAR),
                new BotBrainType("PmcUsec", WildSpawnType.pmcUSEC)
            });
        }

        public static IEnumerable<BotBrainType> AddKnightBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Knight", WildSpawnType.bossKnight) });
        }

        public static IEnumerable<BotBrainType> AddGoonFollowerBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[]
            {
                new BotBrainType("BigPipe", WildSpawnType.followerBigPipe),
                new BotBrainType("BirdEye", WildSpawnType.followerBirdEye)
            });
        }

        public static IEnumerable<BotBrainType> AddCultistBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("SectantWarrior", WildSpawnType.sectantWarrior) });
        }

        public static IEnumerable<BotBrainType> AddCultistPriestBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("SectantPriest", WildSpawnType.sectantPriest) });
        }

        public static IEnumerable<BotBrainType> AddZryachiyBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("BossZryachiy", WildSpawnType.bossZryachiy) });
        }

        public static IEnumerable<BotBrainType> AddZryachiyFollowerBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Fl_Zraychiy", WildSpawnType.followerZryachiy) });
        }

        public static IEnumerable<BotBrainType> AddTagillaBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Tagilla", WildSpawnType.bossTagilla) });
        }

        public static IEnumerable<BotBrainType> AddKillaBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Killa", WildSpawnType.bossKilla) });
        }

        public static IEnumerable<BotBrainType> AddNormalBossBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[]
            {
                new BotBrainType("BossBully", WildSpawnType.bossBully),
                new BotBrainType("BossSanitar", WildSpawnType.bossSanitar),
                new BotBrainType("BossGluhar", WildSpawnType.bossGluhar),
                new BotBrainType("BossKojaniy", WildSpawnType.bossKojaniy),
                new BotBrainType("BossBoar", WildSpawnType.bossBoar),
                new BotBrainType("BossKolontay", WildSpawnType.bossKolontay)
            });
        }

        public static IEnumerable<BotBrainType> AddNormalBossFollowerBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[]
            {
                new BotBrainType("FollowerBully", WildSpawnType.followerBully),
                new BotBrainType("FollowerSanitar", WildSpawnType.followerSanitar),
                new BotBrainType("TagillaFollower", WildSpawnType.followerTagilla),
                new BotBrainType("FollowerGluharAssault", WildSpawnType.followerGluharAssault),
                new BotBrainType("FollowerGluharProtect", WildSpawnType.followerGluharSecurity),
                new BotBrainType("FollowerGluharScout", WildSpawnType.followerGluharScout),
                new BotBrainType("FollowerKojaniy", WildSpawnType.followerKojaniy),
                new BotBrainType("BoarSniper", WildSpawnType.bossBoarSniper),
                new BotBrainType("FlBoar", WildSpawnType.followerBoar),
                new BotBrainType("FlBoarCl", WildSpawnType.followerBoarClose1),
                new BotBrainType("FlBoarSt", WildSpawnType.followerBoarClose2),
                new BotBrainType("FlKlnAslt", WildSpawnType.followerKolontayAssault),
                new BotBrainType("KolonSec", WildSpawnType.followerKolontaySecurity)
            });
        }

        public static IEnumerable<BotBrainType> AddAllNormalBossBrains(this IEnumerable<BotBrainType> list)
        {
            return list
                .AddNormalBossBrains()
                .AddTagillaBrain()
                .AddKillaBrain()
                .AddRogueBrain()
                .AddRaiderBrain()
                .AddKnightBrain()
                .AddBloodhoundBrains();
        }

        public static IEnumerable<BotBrainType> AddAllNormalBossFollowerBrains(this IEnumerable<BotBrainType> list)
        {
            return list
                .AddNormalBossFollowerBrains()
                .AddGoonFollowerBrains();
        }

        public static IEnumerable<BotBrainType> AddAllNormalBossAndFollowerBrains(this IEnumerable<BotBrainType> list)
        {
            return list
                .AddAllNormalBossBrains()
                .AddAllNormalBossFollowerBrains();
        }

        public static IEnumerable<BotBrainType> AddAllCultistBrains(this IEnumerable<BotBrainType> list)
        {
            return list
                .AddCultistBrain()
                .AddCultistPriestBrain();
        }

        public static IEnumerable<BotBrainType> AddZryachiyAndFollowerBrains(this IEnumerable<BotBrainType> list)
        {
            return list
                .AddZryachiyBrain()
                .AddZryachiyFollowerBrain();
        }

        public static IEnumerable<BotBrainType> AddAllSniperBrains(this IEnumerable<BotBrainType> list)
        {
            return list
                .AddSniperScavBrain()
                .AddZryachiyBrain()
                .AddZryachiyFollowerBrain();
        }

        public static IEnumerable<BotBrainType> GetAllNonSniperBrains()
        {
            return Enumerable.Empty<BotBrainType>()
                .AddPMCBrains()
                .AddNormalScavBrains()
                .AddCrazyScavBrain()
                .AddAllNormalBossAndFollowerBrains()
                .AddAllCultistBrains();
        }

        public static IEnumerable<BotBrainType> GetAllBrains()
        {
            return GetAllNonSniperBrains()
                .AddAllSniperBrains();
        }

        public static string[] ToStringArray(this IEnumerable<BotBrainType> list)
        {
            return list.Select(i => i.ToString()).ToArray();
        }

        public static List<string> ToStringList(this IEnumerable<BotBrainType> list)
        {
            return list.Select(i => i.ToString()).ToList();
        }

        public static readonly WildSpawnType[] PMCSpawnTypes = new WildSpawnType[2]
        {
            WildSpawnType.pmcUSEC,
            WildSpawnType.pmcBEAR
        };

        public static bool WillBeAPMC(this BotOwner bot) => bot.Profile.WillBeAPMC();

        public static bool WillBeAPMC(this Profile profile)
        {
            return Enumerable.Empty<BotBrainType>()
                .AddPMCBrains()
                .Any(b => b.SpawnType == profile.Info.Settings.Role);
        }

        public static bool WillBeABoss(this BotOwner botOwner)
        {
            if (botOwner.Profile.Info.Settings.Role == WildSpawnType.assaultGroup)
            {
                return false;
            }

            return botOwner.Profile.Info.Settings.Role.IsBoss();
        }

        public static bool WillBeAPlayerScav(this BotOwner bot) => bot.Profile.WillBeAPlayerScav();

        public static bool WillBeAPlayerScav(this Profile profile)
        {
            // Handle the old version of creating player Scavs
            if (profile.Info.Nickname.Contains(" ("))
            {
                return true;
            }

            // Check for player Scavs created by SPT
            return profile.Info.Settings.Role == WildSpawnType.assault && !string.IsNullOrEmpty(profile.Info.MainProfileNickname);
        }

        public static EPlayerSide GetPlayerSide(this WildSpawnType spawnType)
        {
            if (spawnType == WildSpawnType.pmcUSEC)
            {
                return EPlayerSide.Usec;
            }
            if (spawnType == WildSpawnType.pmcBEAR)
            {
                return EPlayerSide.Bear;
            }

            return EPlayerSide.Savage;
        }

        public static IEnumerable<Player> GetAllHumanAndSimulatedPlayers()
        {
            return Singleton<GameWorld>.Instance.AllAlivePlayersList.HumanAndSimulatedPlayers();
        }

        public static IEnumerable<Player> HumanAndSimulatedPlayers(this IEnumerable<Player> players) => players.HumanAndSimulatedPlayers();
        
        public static IEnumerable<IPlayer> HumanAndSimulatedPlayers(this IEnumerable<IPlayer> players)
        {
            return players.Where(p => p.ShouldPlayerBeTreatedAsHuman());
        }

        public static bool ShouldPlayerBeTreatedAsHuman(this IPlayer player)
        {
            return !player.IsAI || BotGenerator.GetAllGeneratedBotProfileIDs().Contains(player.Profile.Id);
        }

        public static bool ShouldPlayerBeTreatedAsHuman(this BotOwner bot)
        {
            return bot.GetPlayer.ShouldPlayerBeTreatedAsHuman();
        }

        public static bool IsZryachiyOrFollower(this IPlayer player)
        {
            return player.Profile.Info.Settings.Role.IsZryachiyOrFollower();
        }

        public static bool IsZryachiyOrFollower(this WildSpawnType role)
        {
            return (role == WildSpawnType.bossZryachiy) || (role == WildSpawnType.followerZryachiy);
        }

        public static bool IsAlive(this BotOwner bot) => (bot.BotState == EBotState.Active) && !bot.IsDead;

        public static string GetActiveLayerName(this BotOwner bot) => bot.Brain.ActiveLayerName();
        public static string GetActiveLogicName(this BotOwner bot) => bot.Brain.GetActiveNodeReason();

        public static string GetActiveLayerTypeName(this BotOwner bot) => BrainManager.GetActiveLayer(bot)?.GetType()?.Name;
        public static string GetActiveLogicTypeName(this BotOwner bot) => BrainManager.GetActiveLogic(bot)?.GetType()?.Name;

        public static bool IsLayerActive(this BotOwner bot, string layerTypeName) => bot.GetActiveLayerTypeName()?.Equals(layerTypeName) == true;
        public static bool IsLogicActive(this BotOwner bot, string logicTypeName) => bot.GetActiveLogicTypeName()?.Equals(logicTypeName) == true;
    }
}
