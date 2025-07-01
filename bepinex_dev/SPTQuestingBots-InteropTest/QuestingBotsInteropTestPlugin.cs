using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using SPTQuestingBots_InteropTest;

namespace SPTQuestingBotsInteropTest
{
    [BepInPlugin("com.DanW.QuestingBotsInteropTest", "DanW-QuestingBots-InteropTest", "1.2.0")]
    public class QuestingBotsInteropTestPlugin : BaseUnityPlugin
    {
        protected void Awake()
        {
            Logger.LogInfo("Loading QuestingBotsInteropTest...");
            LoggingController.Logger = Logger;
            new GameStartPatch().Enable();
            new BotsControllerSetSettingsPatch().Enable();
            Logger.LogInfo("Loading QuestingBotsInteropTest...done.");
        }
    }
}
