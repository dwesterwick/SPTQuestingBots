using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;

namespace SPTQuestingBotsInteropTest
{
    [BepInPlugin("com.DanW.QuestingBotsInteropTest", "DanW-QuestingBots-InteropTest", "1.1.0")]
    public class QuestingBotsInteropTestPlugin : BaseUnityPlugin
    {
        protected void Awake()
        {
            Logger.LogInfo("Loading QuestingBotsInteropTest...");
            LoggingController.Logger = Logger;
            new GameStartPatch().Enable();
            Logger.LogInfo("Loading QuestingBotsInteropTest...done.");
        }
    }
}
