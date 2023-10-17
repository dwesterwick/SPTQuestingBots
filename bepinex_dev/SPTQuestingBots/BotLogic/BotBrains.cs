using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BotLogic
{
    public enum BotType
    {
        Undetermined,
        Scav,
        PMC,
        Boss
    }

    public static class BotBrains
    {
        public static string SniperScav { get { return "Marksman"; } }
        public static string Raider { get { return "PMC"; } }
        public static string Rogue { get { return "ExUsec"; } }
        public static string Cultist { get { return "SectantWarrior"; } }
        public static string CultistPriest { get { return "SectantPriest"; } }
        public static string Knight { get { return "Knight"; } }
        public static string Tagilla { get { return "Tagilla"; } }
        public static string Killa { get { return "Killa"; } }
        public static string Zryachiy { get { return "BossZryachiy"; } }
        public static string FollowerZryachiy { get { return "Fl_Zraychiy"; } }

        public static IEnumerable<string> NormalScavs => new string[]
        {
            "Assault",
            "CursAssault"
        };

        public static IEnumerable<string> BossesNormal => new string[]
        {
            "BossBully",
            "BossSanitar",
            "BossGluhar",
            "BossKojaniy",
            "BossBoar"
        };

        public static IEnumerable<string> FollowersGoons => new string[]
        {
            "BigPipe",
            "BirdEye"
        };

        public static IEnumerable<string> FollowersNormal => new string[]
        {
            "FollowerBully",
            "FollowerSanitar",
            "TagillaFollower",
            "FollowerGluharAssault",
            "FollowerGluharProtect",
            "FollowerGluharScout",
            "FollowerKojaniy",
            "BoarSniper",
            "FlBoar"
        };

        public static IEnumerable<string> AllNormalBots => NormalScavs.Concat(new[] { Raider, Rogue, Cultist });

        public static IEnumerable<string> AllGoons => FollowersGoons.Concat(new[] { Knight });
        public static IEnumerable<string> BossesWithoutGoonsOrZryachiy => BossesNormal.Concat(new[] { Tagilla, Killa, CultistPriest });
        public static IEnumerable<string> BossesWithoutZryachiy => BossesWithoutGoonsOrZryachiy.Concat(new[] { Knight });
        public static IEnumerable<string> AllBosses => BossesWithoutZryachiy.Concat(new[] { Zryachiy });

        public static IEnumerable<string> AllFollowersWithoutZryachiy => FollowersNormal.Concat(FollowersGoons);

        public static IEnumerable<string> AllBossesWithFollowersWithoutZryachiy => BossesWithoutZryachiy.Concat(AllFollowersWithoutZryachiy);
        public static IEnumerable<string> AllBossesWithFollowers => AllBossesWithFollowersWithoutZryachiy.Concat(new[] { Zryachiy, FollowerZryachiy });

        public static IEnumerable<string> AllBotsExceptSniperScavsAndZryachiy => AllNormalBots.Concat(AllBossesWithFollowersWithoutZryachiy);
        public static IEnumerable<string> AllBotsExceptSniperScavs => AllNormalBots.Concat(AllBossesWithFollowers);
        public static IEnumerable<string> AllBots => AllBotsExceptSniperScavs.Concat(new[] { SniperScav });

        //FollowerGluharAssault and FollowerGluharProtect max layer = 43

        public static readonly WildSpawnType[] pmcSpawnTypes = new WildSpawnType[2]
        {
            (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptUsecValue,
            (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptBearValue
        };

        public static bool WillBotBeAPMC(BotOwner botOwner)
        {
            //LoggingController.LogInfo("Spawn type for bot " + botOwner.Profile.Nickname + ": " + botOwner.Profile.Info.Settings.Role.ToString());
            
            return pmcSpawnTypes
                .Select(t => t.ToString())
                .Contains(botOwner.Profile.Info.Settings.Role.ToString());
        }

        public static bool WillBotBeABoss(BotOwner botOwner)
        {
            return botOwner.Profile.Info.Settings.Role.IsBoss();
        }

        public static EPlayerSide GetSideForWildSpawnType(WildSpawnType spawnType)
        {
            WildSpawnType sptUsec = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptUsecValue;
            WildSpawnType sptBear = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptBearValue;

            //if (spawnType == WildSpawnType.pmcBot || spawnType == sptUsec)
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

        public static ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> GetBrainLayersForBot(BotOwner botOwner)
        {
            ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> emptyCollection = new ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>>(new AICoreLayerClass<BotLogicDecision>[0]);

            // This happens sometimes, and I don't know why
            if (botOwner?.Brain?.BaseBrain == null)
            {
                LoggingController.LogError("Invalid base brain for bot " + botOwner.Profile.Nickname);
                return emptyCollection;
            }

            // Find the field that stores the list of brain layers assigned to the bot
            Type aICoreStrategyClassType = typeof(AICoreStrategyClass<BotLogicDecision>);
            FieldInfo layerListField = aICoreStrategyClassType.GetField("list_0", BindingFlags.NonPublic | BindingFlags.Instance);
            if (layerListField == null)
            {
                LoggingController.LogError("Could not find brain layer list in type " + aICoreStrategyClassType.FullName);
                return emptyCollection;
            }

            // Get the list of brain layers for the bot
            List<AICoreLayerClass<BotLogicDecision>> layerList = (List<AICoreLayerClass<BotLogicDecision>>)layerListField.GetValue(botOwner.Brain.BaseBrain);
            if (layerList == null)
            {
                LoggingController.LogError("Could not retrieve brain layers for bot " + botOwner.Profile.Nickname);
                return emptyCollection;
            }

            return new ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>>(layerList);
        }

        public static IEnumerable<string> GetBrainLayerNamesForBot(BotOwner botOwner)
        {
            ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> brainLayers = GetBrainLayersForBot(botOwner);
            return brainLayers.Select(l => l.Name());
        }

        public static AICoreLayerClass<BotLogicDecision> GetBrainLayerForBot(BotOwner botOwner, string layerName)
        {
            // Get all of the brain layers assigned to the bot
            ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> brainLayers = GetBrainLayersForBot(botOwner);

            // Try to find the matching layer
            IEnumerable<AICoreLayerClass<BotLogicDecision>> matchingLayers = brainLayers.Where(l => l.Name() == layerName);
            if (!matchingLayers.Any())
            {
                return null;
            }

            // Check if multiple layers with the same name exist in the list
            if (matchingLayers.Count() > 1)
            {
                LoggingController.LogWarning("Found multiple brain layers with the name \"" + layerName + "\". Returning the first match.");
            }

            return matchingLayers.First();
        }

        // This checks if the brain layer CAN be used, not if it's currently being used
        public static bool IsBrainLayerActiveForBot(BotOwner botOwner, string layerName)
        {
            AICoreLayerClass<BotLogicDecision> brainLayer = GetBrainLayerForBot(botOwner, layerName);
            if (brainLayer == null)
            {
                //LoggingController.LogWarning("Could not find brain layer with the name \"" + layerName + "\".");
                return false;
            }

            return brainLayer.IsActive;
        }
    }
}
