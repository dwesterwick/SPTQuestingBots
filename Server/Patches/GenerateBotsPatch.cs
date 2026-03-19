using QuestingBots.Helpers;
using QuestingBots.Models;
using QuestingBots.Patches.Internal;
using QuestingBots.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using System.Threading.Tasks;

namespace QuestingBots.Patches
{
    internal class GenerateBotsPatch : AbstractPatch
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
            return typeof(BotCallbacks).GetMethod(nameof(BotCallbacks.GenerateBots))!;
        }

        [PatchPrefix]
        public static async Task<bool> Prefix(ValueTask<string> __result, GenerateBotsRequestData info, MongoId sessionID)
        {
            ConfigUtil configUtil = ServiceRepository.GetService<ConfigUtil>();

            if (!configUtil.CurrentConfig.IsModEnabled())
            {
                return true;
            }

            if (!configUtil.CurrentConfig.BotSpawns.Enabled && !configUtil.CurrentConfig.AdjustPScavChance.Enabled)
            {
                return true;
            }

            GenerateBotsWithPScavFlagRequestData? modifiedInfo = info as GenerateBotsWithPScavFlagRequestData;
            if (modifiedInfo == null)
            {
                return true;
            }

            string json = await GenerateBots(modifiedInfo, sessionID);
            __result = new ValueTask<string>(json);

            return false;
        }

        private static async ValueTask<string> GenerateBots(GenerateBotsWithPScavFlagRequestData info, MongoId sessionID)
        {
            BotController botController = ServiceRepository.GetService<BotController>();
            IEnumerable<BotBase?> bots = await botController.Generate(sessionID, info);

            if (!info.GeneratePScav)
            {
                return SerializeGeneratedBots(bots);
            }

            foreach (BotBase? bot in bots)
            {
                if (bot?.Info?.Settings?.Role == null)
                {
                    throw new InvalidOperationException("A bot with a null role was generated");
                }

                if (bot.Info.Settings.Role != "assault")
                {
                    LoggingUtil loggingUtil = ServiceRepository.GetService<LoggingUtil>();
                    loggingUtil.Info($"Tried generating a player Scav, but a bot with role {bot.Info.Settings.Role} was returned");
                    continue;
                }

                BotNameService botNameService = ServiceRepository.GetService<BotNameService>();
                botNameService.AddRandomPmcNameToBotMainProfileNicknameProperty(bot);

                SetRandomisedGameVersionAndCategory(bot);
            }

            return SerializeGeneratedBots(bots);
        }

        private static void SetRandomisedGameVersionAndCategory(BotBase bot)
        {
            BotGenerator botGenerator = ServiceRepository.GetService<BotGenerator>();
            SetRandomisedGameVersionAndCategoryMethod.Invoke(botGenerator, new object[] { bot });
        }

        private static string SerializeGeneratedBots(IEnumerable<BotBase?> bots)
        {
            HttpResponseUtil httpResponseUtil = ServiceRepository.GetService<HttpResponseUtil>();
            return httpResponseUtil.GetBody(bots);
        }
    }
}
