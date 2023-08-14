using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using DrakiaXYZ.BigBrain.Brains;
using QuestingBots.BotLogic;
using QuestingBots.Controllers;
using UnityEngine;

namespace QuestingBots
{
    [BepInDependency("xyz.drakia.waypoints", "1.2.0")]
    [BepInDependency("xyz.drakia.bigbrain", "0.2.0")]
    [BepInPlugin("com.DanW.QuestingBots", "QuestingBotsPlugin", "0.1.0.0")]
    public class QuestingBotsPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Loading QuestingBotsPlugin...");

            Logger.LogInfo("Loading QuestingBotsPlugin...getting configuration data...");
            ConfigController.GetConfig();
            LoggingController.Logger = Logger;

            if (ConfigController.Config.Enabled)
            {
                LoggingController.LogInfo("Loading QuestingBotsPlugin...enabling patches...");
                new Patches.GameWorldOnDestroyPatch().Enable();
                new Patches.OnGameStartedPatch().Enable();
                
                LoggingController.LogInfo("Loading QuestingBotsPlugin...enabling controllers...");
                this.GetOrAddComponent<BotQuestController>();
                this.GetOrAddComponent<BotGenerator>();

                List<string> botBrainsToChange = BotBrains.AllBots.ToList();
                LoggingController.LogInfo("Loading QuestingBotsPlugin...changing bot brains: " + string.Join(", ", botBrainsToChange));

                BrainManager.AddCustomLayer(typeof(PMCObjectiveLayer), botBrainsToChange, ConfigController.Config.BrainLayerPriority);

                if (ConfigController.Config.Debug.ShowZoneOutlines)
                {
                    this.GetOrAddComponent<PathRender>();
                }
            }

            Logger.LogInfo("Loading QuestingBotsPlugin...done.");
        }
    }
}
