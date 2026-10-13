using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;
using QuestingBots.Helpers;

namespace QuestingBots
{
    public class CreateBroadcasterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(nameof(BotsController.Init), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotsController __instance)
        {
            if (!FikaHelpers.TryGetGameWorld(out GameWorld? gameWorld) || (gameWorld == null))
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Could not attach bot debug broadcaster to GameWorld");
                return;
            }

            gameWorld.gameObject.GetOrAddComponent<BotInfoDataBroadcaster>();
        }
    }
}
