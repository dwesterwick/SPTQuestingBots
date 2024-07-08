using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Models;

namespace SPTQuestingBots.Helpers
{
    public static class BotBrainHelpers
    {
        //FollowerGluharAssault and FollowerGluharProtect max layer = 43

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

        public static readonly WildSpawnType[] pmcSpawnTypes = new WildSpawnType[2]
        {
            (WildSpawnType)SPT.PrePatch.AkiBotsPrePatcher.sptUsecValue,
            (WildSpawnType)SPT.PrePatch.AkiBotsPrePatcher.sptBearValue
        };

        public static bool WillBotBeAPMC(BotOwner botOwner)
        {
            //LoggingController.LogInfo("Spawn type for bot " + botOwner.GetText() + ": " + botOwner.Profile.Info.Settings.Role.ToString());

            return pmcSpawnTypes
                .Select(t => t.ToString())
                .Contains(botOwner.Profile.Info.Settings.Role.ToString());
        }

        public static bool WillBotBeABoss(BotOwner botOwner)
        {
            if (botOwner.Profile.Info.Settings.Role == WildSpawnType.assaultGroup)
            {
                return false;
            }

            return botOwner.Profile.Info.Settings.Role.IsBoss();
        }

        public static EPlayerSide GetSideForWildSpawnType(WildSpawnType spawnType)
        {
            WildSpawnType sptUsec = (WildSpawnType)SPT.PrePatch.AkiBotsPrePatcher.sptUsecValue;
            WildSpawnType sptBear = (WildSpawnType)SPT.PrePatch.AkiBotsPrePatcher.sptBearValue;

            if (spawnType == sptUsec)
            {
                return EPlayerSide.Usec;
            }
            else if (spawnType == sptBear)
            {
                return EPlayerSide.Bear;
            }
            else
            {
                return EPlayerSide.Savage;
            }
        }

        public static bool ShouldPlayerBeTreatedAsHuman(this IPlayer player)
        {
            if (!player.IsAI)
            {
                return true;
            }

            string[] generatedBotIDs = BotGenerator.GetAllGeneratedBotProfileIDs().ToArray();
            return generatedBotIDs.Contains(player.Profile.Id);
        }

        public static bool ShouldPlayerBeTreatedAsHuman(this BotOwner bot)
        {
            return bot.GetPlayer.ShouldPlayerBeTreatedAsHuman();
        }
    }
}
