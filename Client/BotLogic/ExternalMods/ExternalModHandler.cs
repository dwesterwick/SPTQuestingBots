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
using QuestingBots.BotLogic.ExternalMods.Functions.NetworkTransactions;
using QuestingBots.BotLogic.ExternalMods.LoadedModInfo;
using QuestingBots.Configuration;
using QuestingBots.Utils;

namespace QuestingBots.BotLogic.ExternalMods
{
    public static class ExternalModHandler
    {
        public static SAINModInfo SAINModInfo { get; private set; } = new SAINModInfo();
        public static LootingBotsModInfo LootingBotsModInfo { get; private set; } = new LootingBotsModInfo();
        public static FikaModInfo FikaModInfo { get; private set; } = new FikaModInfo();
        public static FikaHeadlessModInfo FikaHeadlessModInfo { get; private set; } = new FikaHeadlessModInfo();
        public static QuestingBotsFikaSyncModInfo QuestingBotsFikaSyncModInfo { get; private set; } = new QuestingBotsFikaSyncModInfo();

        private static List<AbstractExternalModInfo> externalMods = new List<AbstractExternalModInfo>
        {
            SAINModInfo,
            LootingBotsModInfo,
            FikaModInfo,
            FikaHeadlessModInfo,
            QuestingBotsFikaSyncModInfo
        };

        public static AbstractExtractFunction CreateExtractFunction(this BotOwner _botOwner) => SAINModInfo.CreateExtractFunction(_botOwner);
        public static AbstractHearingFunction CreateHearingFunction(this BotOwner _botOwner) => SAINModInfo.CreateHearingFunction(_botOwner);
        public static AbstractLootFunction CreateLootFunction(this BotOwner _botOwner) => LootingBotsModInfo.CreateLootFunction(_botOwner);
        public static AbstractRunNetworkTransactionsFunction CreateRunNetworkTransactionsFunction(this BotOwner _botOwner) => QuestingBotsFikaSyncModInfo.CreateRunNetworkTransactionsFunction(_botOwner);

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
