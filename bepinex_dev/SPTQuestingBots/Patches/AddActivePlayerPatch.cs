using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class AddActivePlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod("AddActivePLayer", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            if (Singleton<GameWorld>.Instance.gameObject.TryGetComponent(out Components.LocationData oldLocationData))
            {
                LoggingController.LogWarning("There is already a LocationData component added to the current GameWorld instance.");
                return;
            }

            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<Components.LocationData>();

            if (ConfigController.Config.BotSpawns.DelayGameStartUntilBotGenFinishes)
            {
                Spawning.GameStartPatch.ClearMissedWaves();
                Spawning.GameStartPatch.IsDelayingGameStart = true;
            }
        }
    }
}
