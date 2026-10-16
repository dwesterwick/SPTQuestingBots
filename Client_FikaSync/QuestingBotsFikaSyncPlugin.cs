using BepInEx;
using BepInEx.Logging;
using QuestingBots.ExternalMods.Functions.Multiplayer;
using QuestingBots.Helpers;
using QuestingBots.PacketHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots
{
    [BepInDependency("com.fika.core", "2.4.3")]
    [BepInDependency(ModInfo.GUID, "1.1.0")]
    [BepInPlugin(ModInfo.GUID + "fikasync", ModInfo.MODNAME + "FikaSync", ModInfo.MOD_VERSION)]
    internal class QuestingBotsFikaSyncPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource PluginLogger = null!;

        protected void Awake()
        {
            PluginLogger = Logger;
            PluginLogger.LogInfo($"{nameof(QuestingBotsFikaSyncPlugin)} has been loaded.");

            FikaMultiplayerFunctions.SetGetPayerFunc(FikaHelpers.GetPlayer);
            FikaMultiplayerFunctions.SetGetAlivePayersFunc(FikaHelpers.GetPlayers);
            FikaMultiplayerFunctions.SetGetAliveHumanPayersFunc(FikaHelpers.GetHumanPlayers);

            AbstractPacketHandler.SetupAllPacketHandlers();
        }
    }
}
