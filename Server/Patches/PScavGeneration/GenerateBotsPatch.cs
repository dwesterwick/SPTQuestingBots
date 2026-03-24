using QuestingBots.Models;
using QuestingBots.Patches.Internal;
using QuestingBots.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.Models.Eft.Bot;
using System.Reflection;
using System.Threading.Tasks;

namespace QuestingBots.Patches.PScavGeneration
{
    internal class GenerateBotsPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotCallbacks).GetMethod(nameof(BotCallbacks.GenerateBots))!;
        }

        [PatchPrefix]
        public static void PatchPrefix(ref GenerateBotsRequestData info)
        {
            GenerateBotsWithPScavFlagRequestData? modifiedInfo = info as GenerateBotsWithPScavFlagRequestData;
            if (modifiedInfo == null)
            {
                LoggingUtil loggingUtil = ServiceRepository.GetService<LoggingUtil>();
                loggingUtil.Error($"GenerateBotsRequestData was not in the expected format for Questing Bots. Falling back to default SPT behavior.");

                return;
            }

            modifiedInfo.AddPScavFlagToConditions();
            info = modifiedInfo;
        }
    }
}
