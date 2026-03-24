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

namespace QuestingBots.Patches.PScavGeneration
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
            GenerateConditionWithPScavFlag? modifiedCondition = generateRequest as GenerateConditionWithPScavFlag;
            if (modifiedCondition == null)
            {
                LoggingUtil loggingUtil = ServiceRepository.GetService<LoggingUtil>();
                loggingUtil.Error($"GenerateCondition did not contain the required GeneratePScav flag. Falling back to default SPT behavior.");

                return;
            }

            if (!modifiedCondition.GeneratePScav)
            {
                return;
            }

            __result = ConvertAllToPScav(__result, modifiedCondition.Limit);
        }

        private static IEnumerable<BotBase?> ConvertAllToPScav(IEnumerable<BotBase?> bots, int targetCount)
        {
            LoggingUtil loggingUtil = ServiceRepository.GetService<LoggingUtil>();

            List<BotBase> UpdatedBots = new List<BotBase>();
            int convertedBots = 0;

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
                    convertedBots++;
                }

                UpdatedBots.Add(bot);
            }

            if (convertedBots < targetCount)
            {
                loggingUtil.Warning($"{targetCount} player Scavs were requested, but only {convertedBots} were created");
            }

            return UpdatedBots;
        }

        private static bool CanConvertToPScav(BotBase bot)
        {
            if (bot?.Info?.Settings?.Role == null)
            {
                LoggingUtil loggingUtil = ServiceRepository.GetService<LoggingUtil>();
                loggingUtil.Error("A bot with a null role was generated");

                return false;
            }

            if (bot.Info.Settings.Role != "assault")
            {
                //LoggingUtil loggingUtil = ServiceRepository.GetService<LoggingUtil>();
                //loggingUtil.Warning($"Tried generating a player Scav, but a bot with role {bot.Info.Settings.Role} was returned");

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
