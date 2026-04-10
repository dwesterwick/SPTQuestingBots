using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using QuestingBots_InteropTest;

namespace QuestingBotsInteropTest
{
    [BepInPlugin("com.danw.questingbotsinteroptest", "QuestingBots-InteropTest", "1.3.0")]
    public class QuestingBotsInteropTestPlugin : BaseUnityPlugin
    {
        protected void Awake()
        {
            Logger.LogInfo("Loading QuestingBotsInteropTest...");
            LoggingController.Logger = Logger;
            new GameStartPatch().Enable();
            new BotsControllerInitPatch().Enable();
            Logger.LogInfo("Loading QuestingBotsInteropTest...done.");
        }
    }
}
