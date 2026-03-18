using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Utils;

namespace QuestingBots.Patches
{
    public class BotsControllerSetSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(nameof(BotsController.SetSettings), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            if (Singleton<GameWorld>.Instance.gameObject.TryGetComponent(out Components.LocationData oldLocationData))
            {
                Singleton<LoggingUtil>.Instance.LogError("There is already a LocationData component added to the current GameWorld instance.");
                return;
            }

            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<Components.LocationData>();

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.DelayGameStartUntilBotGenFinishes)
            {
                Spawning.GameStartPatch.ClearMissedWaves();
                Spawning.GameStartPatch.IsDelayingGameStart = true;

                Singleton<LoggingUtil>.Instance.LogInfo("Delaying the game start until bot generation finishes...");
            }
        }
    }
}
