using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using QuestingBots.Components.Spawning;
using QuestingBots.Helpers;
using QuestingBots.Patches.Spawning;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.PacketHandlers
{
    internal class BotGeneratorProgressHandler : AbstractPacketHandler
    {
        public BotGeneratorProgressHandler() { }

        internal override void Setup()
        {
            GameStartPatch.OnWaitForBotGeneratorsProgressChanged += OnWaitForBotGeneratorsProgressChanged;
        }

        internal override void RegisterPackets(FikaClient client)
        {
            client.RegisterPacket<BotGeneratorProgressPacket>(OnBotGeneratorProgressPacketReceived);
        }

        private static void OnWaitForBotGeneratorsProgressChanged()
        {
            BotGeneratorProgressPacket packet = new BotGeneratorProgressPacket()
            {
                RemainingBotGenerators = BotGenerator.RemainingBotGenerators,
                CurrentBotGeneratorType = BotGenerator.CurrentBotGeneratorType,
                CurrentBotGeneratorProgress = BotGenerator.CurrentBotGeneratorProgress
            };

            if (!packet.TrySendToAllClients())
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Could not send BotGeneratorProgressPacket");
            }
        }

        private static void OnBotGeneratorProgressPacketReceived(BotGeneratorProgressPacket packet)
        {
            if (!FikaBackendUtils.IsClient)
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogWarning("Ignoring BotGeneratorProgressPacket for the host machine");
                return;
            }

            if (packet.RemainingBotGenerators > 0)
            {
                string screenMessageText = TimeHasComeScreenClassChangeStatusPatch.GetWaitingForGeneratorsText(packet.CurrentBotGeneratorType);
                TimeHasComeScreenClassChangeStatusPatch.ChangeStatus(screenMessageText, packet.CurrentBotGeneratorProgress / 100f);
            }
            else
            {
                TimeHasComeScreenClassChangeStatusPatch.RestorePreviousStatus();
            }
        }
    }
}
