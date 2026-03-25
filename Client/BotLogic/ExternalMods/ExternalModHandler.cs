using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.ExternalMods.Functions.Extract;
using QuestingBots.BotLogic.ExternalMods.Functions.Hearing;
using QuestingBots.BotLogic.ExternalMods.Functions.Loot;
using QuestingBots.BotLogic.ExternalMods.ModInfo;
using QuestingBots.Configuration;
using QuestingBots.Controllers;
using QuestingBots.Utils;

namespace QuestingBots.BotLogic.ExternalMods
{
    public static class ExternalModHandler
    {
        public static SAINModInfo SAINModInfo { get; private set; } = new SAINModInfo();
        public static LootingBotsModInfo LootingBotsModInfo { get; private set; } = new LootingBotsModInfo();
        public static DonutsModInfo DonutsModInfo { get; private set; } = new DonutsModInfo();
        public static PerformanceImprovementsModInfo PerformanceImprovementsModInfo { get; private set; } = new PerformanceImprovementsModInfo();
        public static PleaseJustFightModInfo PleaseJustFightModInfo { get; private set; } = new PleaseJustFightModInfo();
        public static FikaModInfo FikaModInfo { get; private set; } = new FikaModInfo();
        public static HeadlessModInfo HeadlessModInfo { get; private set; } = new HeadlessModInfo();

        private static List<AbstractExternalModInfo> externalMods = new List<AbstractExternalModInfo>
        {
            SAINModInfo,
            LootingBotsModInfo,
            DonutsModInfo,
            PerformanceImprovementsModInfo,
            PleaseJustFightModInfo,
            FikaModInfo,
            HeadlessModInfo
        };

        public static AbstractExtractFunction CreateExtractFunction(this BotOwner _botOwner) => SAINModInfo.CreateExtractFunction(_botOwner);
        public static AbstractHearingFunction CreateHearingFunction(this BotOwner _botOwner) => SAINModInfo.CreateHearingFunction(_botOwner);
        public static AbstractLootFunction CreateLootFunction(this BotOwner _botOwner) => LootingBotsModInfo.CreateLootFunction(_botOwner);

        public static int GetMinimumCombatLayerPriority(string _brainName) => SAINModInfo.GetMinimumLayerPriority(_brainName);
        public static MinMaxConfig GetSearchTimeAfterCombat(string _brainName) => SAINModInfo.GetSearchTimeAfterCombat(_brainName);

        public static void CheckForExternalMods()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Enabled)
            {
                return;
            }

            foreach (AbstractExternalModInfo modInfo in externalMods)
            {
                if (!modInfo.CheckIfInstalled())
                {
                    continue;
                }

                Singleton<LoggingUtil>.Instance.LogInfo($"Found external mod {modInfo.GetName()} (version {modInfo.GetVersion()})");

                if (!modInfo.IsCompatible())
                {
                    Chainloader.DependencyErrors.Add(modInfo.IncompatibilityMessage);
                    continue;
                }

                if (!modInfo.CheckInteropAvailability())
                {
                    Singleton<LoggingUtil>.Instance.LogWarning($"Interoperability for external mod {modInfo.GUID} could not be initialized");
                }
            }
        }
    }
}
