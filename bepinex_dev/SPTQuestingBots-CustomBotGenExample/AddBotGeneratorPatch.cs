using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;

namespace SPTQuestingBots_CustomBotGenExample
{
    public class AddBotGeneratorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SPTQuestingBots.Components.LocationData).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix()
        {
            if (!QuestingBotsCustomBotGenExamplePlugin.Enabled.Value)
            {
                return;
            }

            if (!SPTQuestingBots.Controllers.ConfigController.Config.BotSpawns.Enabled)
            {
                LoggingController.LogError("Cannot generate bots if Questing Bots's bot-spawning system is disabled");
                return;
            }

            Singleton<GameWorld>.Instance.gameObject.AddComponent<TestBotGenerator>();
        }
    }
}
