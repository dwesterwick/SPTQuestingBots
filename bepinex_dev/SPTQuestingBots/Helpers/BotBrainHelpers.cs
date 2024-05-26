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
            return list.Concat(new[] { new BotBrainType("BossTest") });
        }

        public static IEnumerable<BotBrainType> AddBTRBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("BTR") });
        }

        public static IEnumerable<BotBrainType> AddNormalScavBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[]
            {
                new BotBrainType("Assault"),
                new BotBrainType("CursAssault")
            });
        }

        public static IEnumerable<BotBrainType> AddSniperScavBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Marksman") });
        }

        public static IEnumerable<BotBrainType> AddBloodhoundBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("ArenaFighter") });
        }

        public static IEnumerable<BotBrainType> AddCrazyScavBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Obdolbs") });
        }

        public static IEnumerable<BotBrainType> AddSantaBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Gifter") });
        }

        public static IEnumerable<BotBrainType> AddRogueBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("ExUsec") });
        }

        public static IEnumerable<BotBrainType> AddRaiderBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("PMC") });
        }

        public static IEnumerable<BotBrainType> AddKnightBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Knight") });
        }

        public static IEnumerable<BotBrainType> AddGoonFollowerBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[]
            {
                new BotBrainType("BigPipe"),
                new BotBrainType("BirdEye")
            });
        }

        public static IEnumerable<BotBrainType> AddCultistBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("SectantWarrior") });
        }

        public static IEnumerable<BotBrainType> AddCultistPriestBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("SectantPriest") });
        }

        public static IEnumerable<BotBrainType> AddZryachiyBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("BossZryachiy") });
        }

        public static IEnumerable<BotBrainType> AddZryachiyFollowerBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Fl_Zraychiy") });
        }

        public static IEnumerable<BotBrainType> AddTagillaBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Tagilla") });
        }

        public static IEnumerable<BotBrainType> AddKillaBrain(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[] { new BotBrainType("Killa") });
        }

        public static IEnumerable<BotBrainType> AddNormalBossBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[]
            {
                new BotBrainType("BossBully"),
                new BotBrainType("BossSanitar"),
                new BotBrainType("BossGluhar"),
                new BotBrainType("BossKojaniy"),
                new BotBrainType("BossBoar"),
                new BotBrainType("BossKolontay")
            });
        }

        public static IEnumerable<BotBrainType> AddNormalBossFollowerBrains(this IEnumerable<BotBrainType> list)
        {
            return list.Concat(new[]
            {
                new BotBrainType("FollowerBully"),
                new BotBrainType("FollowerSanitar"),
                new BotBrainType("TagillaFollower"),
                new BotBrainType("FollowerGluharAssault"),
                new BotBrainType("FollowerGluharProtect"),
                new BotBrainType("FollowerGluharScout"),
                new BotBrainType("FollowerKojaniy"),
                new BotBrainType("BoarSniper"),
                new BotBrainType("FlBoar"),
                new BotBrainType("FlKlnAslt"),
                new BotBrainType("KolonSec")
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
            (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptUsecValue,
            (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptBearValue
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
            // Probably should enable this after the 0.5.0 release. 
            /*if (botOwner.Profile.Info.Settings.Role == WildSpawnType.assaultGroup)
            {
                return false;
            }*/

            return botOwner.Profile.Info.Settings.Role.IsBoss();
        }

        public static EPlayerSide GetSideForWildSpawnType(WildSpawnType spawnType)
        {
            WildSpawnType sptUsec = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptUsecValue;
            WildSpawnType sptBear = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptBearValue;

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
