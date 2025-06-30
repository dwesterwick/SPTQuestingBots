using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using EFT;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Extract;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Hearing;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Loot;
using SPTQuestingBots.BotLogic.ExternalMods.ModInfo;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BotLogic.ExternalMods
{
    public static class ExternalModHandler
    {
        public static SAINModInfo SAINModInfo { get; private set; } = new SAINModInfo();
        public static LootingBotsModInfo LootingBotsModInfo { get; private set; } = new LootingBotsModInfo();
        public static DonutsModInfo DonutsModInfo { get; private set; } = new DonutsModInfo();
        public static PerformanceImprovementsModInfo PerformanceImprovementsModInfo { get; private set; } = new PerformanceImprovementsModInfo();
        public static PleaseJustFightModInfo PleaseJustFightModInfo { get; private set; } = new PleaseJustFightModInfo();

        private static List<AbstractExternalModInfo> externalMods = new List<AbstractExternalModInfo>
        {
            SAINModInfo,
            LootingBotsModInfo,
            DonutsModInfo,
            PerformanceImprovementsModInfo,
            PleaseJustFightModInfo
        };

        public static AbstractExtractFunction CreateExtractFunction(this BotOwner _botOwner) => SAINModInfo.CreateExtractFunction(_botOwner);
        public static AbstractHearingFunction CreateHearingFunction(this BotOwner _botOwner) => SAINModInfo.CreateHearingFunction(_botOwner);
        public static AbstractLootFunction CreateLootFunction(this BotOwner _botOwner) => LootingBotsModInfo.CreateLootFunction(_botOwner);

        public static int GetMinimumCombatLayerPriority(string _brainName) => SAINModInfo.GetMinimumLayerPriority(_brainName);
        public static MinMaxConfig GetSearchTimeAfterCombat(string _brainName) => SAINModInfo.GetSearchTimeAfterCombat(_brainName);

        public static void CheckForExternalMods()
        {
            if (!ConfigController.Config.Enabled)
            {
                return;
            }

            foreach (AbstractExternalModInfo modInfo in externalMods)
            {
                if (!modInfo.CheckIfInstalled())
                {
                    continue;
                }

                LoggingController.LogInfo($"Found external mod {modInfo.GetName()} (version {modInfo.GetVersion()})");

                if (!modInfo.IsCompatible())
                {
                    Chainloader.DependencyErrors.Add(modInfo.IncompatibilityMessage);
                    continue;
                }

                if (!modInfo.CheckInteropAvailability())
                {
                    LoggingController.LogWarning($"Interoperability for external mod {modInfo.GUID} could not be initialized");
                }
            }
        }
    }
}
