using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace QuestingBots.BotLogic
{
    public static class BotBrains
    {
        public static string PMC { get { return "PMC"; } }
        public static string Cultist { get { return "SectantWarrior"; } }
        public static string Knight { get { return "Knight"; } }
        public static string Rogue { get { return "ExUsec"; } }
        public static string Tagilla { get { return "Tagilla"; } }
        public static string Killa { get { return "Killa"; } }

        public static IEnumerable<string> Scavs => new string[] { "Assault", "CursAssault" };
        public static IEnumerable<string> BossesNormal => new string[] { "BossBully","BossSanitar", "Tagilla", "BossGluhar", "BossKojaniy", "SectantPriest" };
        public static IEnumerable<string> FollowersGoons => new string[] { "BigPipe", "BirdEye" };
        public static IEnumerable<string> FollowersNormal => new string[] { "FollowerBully", "FollowerSanitar", "TagillaFollower", "FollowerGluharAssault", "FollowerGluharProtect", "FollowerGluharScout" };

        public static IEnumerable<string> AllGoons => FollowersGoons.Concat(new[] { Knight });
        public static IEnumerable<string> BossesWithoutGoons => BossesNormal.Concat(new[] { Tagilla, Killa });
        public static IEnumerable<string> BossesWithKnight => BossesWithoutGoons.Concat(new[] { Knight });

        public static IEnumerable<string> AllNormalBots => Scavs.Concat(new[] { PMC, Rogue, Cultist });
        public static IEnumerable<string> AllFollowers => FollowersNormal.Concat(FollowersGoons);
        public static IEnumerable<string> AllBossesWithFollowers => BossesWithKnight.Concat(AllFollowers);
        public static IEnumerable<string> AllBots => AllNormalBots.Concat(AllBossesWithFollowers);

        //FollowerGluharAssault and FollowerGluharProtect max layer = 43

        public static readonly WildSpawnType[] pmcSpawnTypes = new WildSpawnType[2]
        {
            (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptUsecValue,
            (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptBearValue
        };

        public static bool WillBotBeAPMC(BotOwner botOwner)
        {
            LoggingController.LogInfo("Spawn type for bot " + botOwner.Profile.Nickname + ": " + botOwner.Profile.Info.Settings.Role.ToString());
            return pmcSpawnTypes.Select(t => t.ToString()).Contains(botOwner.Profile.Info.Settings.Role.ToString());
        }

        public static EPlayerSide GetSideForWildSpawnType(WildSpawnType spawnType)
        {
            WildSpawnType sptUsec = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptUsecValue;
            WildSpawnType sptBear = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptBearValue;

            if (spawnType == WildSpawnType.pmcBot || spawnType == sptUsec)
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

            ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> emptyCollection = new ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>>(new List<AICoreLayerClass<BotLogicDecision>>());

            Type aICoreStrategyClassType = typeof(AICoreStrategyClass<BotLogicDecision>);
            FieldInfo layerListField = aICoreStrategyClassType.GetField("list_0", BindingFlags.NonPublic | BindingFlags.Instance);
            if (layerListField == null)
            {
                LoggingController.LogError("Could not find brain layer list in type " + aICoreStrategyClassType.FullName);
                return emptyCollection;
            }

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
            ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> brainLayers = GetBrainLayersForBot(botOwner);
            IEnumerable<AICoreLayerClass<BotLogicDecision>> matchingLayers = brainLayers.Where(l => l.Name() == layerName);
            if (!matchingLayers.Any())
            {
                return null;
            }

            if (matchingLayers.Count() > 1)
            {
                LoggingController.LogWarning("Found multiple brain layers with the name \"" + layerName + "\". Returning the first match.");
            }

            return matchingLayers.First();
        }

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
