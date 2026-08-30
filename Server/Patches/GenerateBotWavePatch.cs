using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services.Bot;
using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace QuestingBots.Patches
{
    [Injectable]
    public class GenerateBotWavePatch : AbstractPatch
    {
        private static LoggingUtil _loggingUtil = null!;
        private static BotNameService _botNameService = null!;
        private static BotGenerator _botGenerator = null!;

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

        public GenerateBotWavePatch(LoggingUtil loggingUtil, BotNameService botNameService, BotGenerator botGenerator)
        {
            _loggingUtil = loggingUtil;
            _botNameService = botNameService;
            _botGenerator = botGenerator;
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotController).GetMethod("GenerateBotWave", BindingFlags.Instance | BindingFlags.NonPublic)!;
        }

        [PatchPostfix]
        public static void PatchPostfix(ref IEnumerable<BotBase?> __result, GenerateCondition generateRequest)
        {
            if (!generateRequest.ExtensionData!.TryGetValue("GeneratePScav", out var generatePScavObj))
            {
                _loggingUtil.Error("GenerateCondition did not contain the required GeneratePScav flag. Falling back to default SPT behavior.");

                return;
            }

            if (generatePScavObj is JsonElement generatePScavElement && generatePScavElement.GetBoolean())
            {
                __result = ConvertAllToPScav(__result, generateRequest.Limit);
            }
        }

        private static List<BotBase?> ConvertAllToPScav(IEnumerable<BotBase?> bots, int targetCount)
        {
            List<BotBase?> UpdatedBots = new List<BotBase?>();
            int convertedBots = 0;

            foreach (BotBase? bot in bots)
            {
                if (bot == null)
                {
                    _loggingUtil.Error("A null bot was generated");
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
                _loggingUtil.Warning($"{targetCount} player Scavs were requested, but only {convertedBots} were created");
            }

            return UpdatedBots;
        }

        private static bool CanConvertToPScav(BotBase bot)
        {
            if (bot.Info?.Settings?.Role == null)
            {
                _loggingUtil.Error("A bot with a null role was generated");

                return false;
            }

            if (bot.Info.Settings.Role != "assault")
            {
                //_loggingUtil.Warning($"Tried generating a player Scav, but a bot with role {bot.Info.Settings.Role} was returned");

                return false;
            }

            return true;
        }

        private static void ConvertToPScav(BotBase bot)
        {
            _botNameService.AddRandomPmcNameToBotMainProfileNicknameProperty(bot);

            SetRandomisedGameVersionAndCategory(bot);
        }

        private static void SetRandomisedGameVersionAndCategory(BotBase bot)
        {
            SetRandomisedGameVersionAndCategoryMethod.Invoke(_botGenerator, new object?[] { bot.Info });
        }
    }
}
