using Comfort.Common;
using EFT;
using QuestingBots.Components;
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
    public class BotsControllerInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(nameof(BotsController.Init), BindingFlags.Public | BindingFlags.Instance);
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
            GetOrAddCommonGameWorldComponents();

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.DelayGameStartUntilBotGenFinishes)
            {
                Spawning.GameStartPatch.DelayGameStart();

                if (QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.SpawningAndDying))
                {
                    Singleton<LoggingUtil>.Instance.LogInfo("Delaying the game start until bot generation finishes...");
                }
            }
        }

        public static void GetOrAddCommonGameWorldComponents()
        {
            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<DebugData>();

            PlayerData playerData = Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<PlayerData>();
            Singleton<PlayerData>.Create(playerData);
        }
    }
}
