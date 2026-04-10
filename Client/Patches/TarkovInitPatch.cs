using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using QuestingBots.BotLogic.ExternalMods;
using QuestingBots.Components;
using QuestingBots.Helpers;
using QuestingBots.Models;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches
{
    public class TarkovInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.Init), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(TarkovApplication __instance, IAssetsManager assetsManager, InputTree inputTree)
        {
            checkSPTVersion();

            ExternalModHandler.CheckForExternalMods();

            addQuestingBotsBrainLayers();

            __instance.GetOrAddComponent<QuestValidationComponent>();
        }

        private static void checkSPTVersion()
        {
            SemanticVersionRange sptValidRange = SemanticVersionRange.Parse(ModInfo.SPT_VERSION_COMPATIBILITY);
            Version MinVersion = sptValidRange.MinVersion;
            Version MaxVersion = sptValidRange.MaxVersion;

            if (Helpers.GameCompatibilityCheckHelper.IsSPTWithinVersionRange(MinVersion, MaxVersion, out string currentVersion))
            {
                return;
            }

            string errorMessage = "Could not load " + ModInfo.MODNAME + " because it requires SPT between version " + MinVersion.ToString() + " and " + MaxVersion.ToString();
            errorMessage += ". The current version is " + currentVersion + ".";

            Chainloader.DependencyErrors.Add(errorMessage);
        }

        private static void addQuestingBotsBrainLayers()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Enabled)
            {
                return;
            }

            if (ExternalModHandler.SAINModInfo.IsInstalled)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("SAIN detected. Adjusting Questing Bots brain layer priorities...");
                BotBrainHelpers.AddQuestingBotsBrainLayers(Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BrainLayerPriorities.WithSAIN);
            }
            else
            {
                BotBrainHelpers.AddQuestingBotsBrainLayers(Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BrainLayerPriorities.WithoutSAIN);
            }
        }
    }
}
