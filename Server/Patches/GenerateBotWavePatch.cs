using QuestingBots.Helpers;
using QuestingBots.Models;
using QuestingBots.Patches.Internal;
using QuestingBots.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using System.Collections;
using System.Reflection;

namespace QuestingBots.Patches
{
    public class GenerateBotWavePatch : AbstractPatch
    {
        private static MethodInfo _setRandomisedGameVersionAndCategoryMethod = null!;
        public static MethodInfo SetRandomisedGameVersionAndCategoryMethod
        {
            get
            {
                if (_setRandomisedGameVersionAndCategoryMethod == null)
                {
                    _setRandomisedGameVersionAndCategoryMethod = GetSetRandomisedGameVersionAndCategoryMethod();
                }

                return _setRandomisedGameVersionAndCategoryMethod;
            }
        }

        private static MethodInfo GetSetRandomisedGameVersionAndCategoryMethod()
        {
            string methodName = "SetRandomisedGameVersionAndCategory";
            MethodInfo? setRandomisedGameVersionAndCategoryMethod = typeof(BotGenerator).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (setRandomisedGameVersionAndCategoryMethod == null)
            {
                throw new InvalidOperationException($"Cannot find method {methodName} in BotGenerator");
            }

            return setRandomisedGameVersionAndCategoryMethod;
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotController).GetMethod("GenerateBotWave", BindingFlags.Instance | BindingFlags.NonPublic)!;
        }

        [PatchPostfix]
        public static void PatchPostfix(ref IEnumerable<BotBase?> __result, GenerateCondition generateRequest)
        {
            ConfigUtil configUtil = ServiceRepository.GetService<ConfigUtil>();

            if (!configUtil.CurrentConfig.IsModEnabled())
            {
                return;
            }

            if (!configUtil.CurrentConfig.BotSpawns.Enabled && !configUtil.CurrentConfig.AdjustPScavChance.Enabled)
            {
                return;
            }

            LoggingUtil loggingUtil = ServiceRepository.GetService<LoggingUtil>();

            GenerateConditionWithPScavFlag? modifiedCondition = generateRequest as GenerateConditionWithPScavFlag;
            if (modifiedCondition == null)
            {
                loggingUtil.Error($"GenerateCondition did not contain the required GeneratePScav flag. Falling back to default SPT behavior.");
                return;
            }

            if (!modifiedCondition.GeneratePScav)
            {
                return;
            }

            __result = ConvertAllToPScav(__result);
        }

        private static IEnumerable<BotBase?> ConvertAllToPScav(IEnumerable<BotBase?> bots)
        {
            LoggingUtil loggingUtil = ServiceRepository.GetService<LoggingUtil>();

            List<BotBase> convertedBots = new List<BotBase>();
            foreach (BotBase? bot in bots)
            {
                if (bot == null)
                {
                    loggingUtil.Error("A null bot was generated");
                    continue;
                }

                if (CanConvertToPScav(bot))
                {
                    ConvertToPScav(bot);
                }

                convertedBots.Add(bot);
            }

            return convertedBots;
        }

        private static bool CanConvertToPScav(BotBase bot)
        {
            LoggingUtil loggingUtil = ServiceRepository.GetService<LoggingUtil>();

            if (bot?.Info?.Settings?.Role == null)
            {
                loggingUtil.Error("A bot with a null role was generated");
                return false;
            }

            if (bot.Info.Settings.Role != "assault")
            {
                loggingUtil.Warning($"Tried generating a player Scav, but a bot with role {bot.Info.Settings.Role} was returned");
                return false;
            }

            return true;
        }

        private static void ConvertToPScav(BotBase bot)
        {
            BotNameService botNameService = ServiceRepository.GetService<BotNameService>();
            botNameService.AddRandomPmcNameToBotMainProfileNicknameProperty(bot);

            SetRandomisedGameVersionAndCategory(bot);
        }

        private static void SetRandomisedGameVersionAndCategory(BotBase bot)
        {
            BotGenerator botGenerator = ServiceRepository.GetService<BotGenerator>();
            SetRandomisedGameVersionAndCategoryMethod.Invoke(botGenerator, new object?[] { bot.Info });
        }
    }
}
