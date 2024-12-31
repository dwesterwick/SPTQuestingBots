using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class LighthouseTraderZonePlayerAttackPatch : ModulePatch
    {
        private static string lightkeeperTraderId = "638f541a29ffd1183d187f57";

        protected override MethodBase GetTargetMethod()
        {
            return typeof(LighthouseTraderZone).GetMethod("method_7", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(Player player, IPlayer lastAgressor, PhysicsTriggerHandler ___physicsTriggerHandler_0)
        {
            if (player == null || lastAgressor == null)
            {
                return;
            }

            if (player.Profile.Id == lastAgressor.Profile.Id)
            {
                return;
            }

            if (lastAgressor.IsAI)
            {
                return;
            }

            // The victim already killed another player on the island
            if (player.IsAgressorInLighthouseTraderZone)
            {
                return;
            }

            // Ignore victims that are not on the island
            if (!___physicsTriggerHandler_0.trigger.bounds.Contains(player.Position))
            {
                LoggingController.LogWarning("[DSP Not Changed] Victim not on the island");
                return;
            }

            // If the aggressor doesn't have a DSP, there's nothing to do
            Player lastAgressorPlayer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(lastAgressor.ProfileId);
            if (!lastAgressorPlayer.RecodableItemsHandler.TryToGetRecodableComponent(out RadioTransmitterRecodableComponent radioTransmitterRecodableComponent))
            {
                LoggingController.LogWarning("[DSP Not Changed] Aggressor does not have a DSP");
                return;
            }

            // If the aggressor's DSP isn't encoded, there's nothing to do
            RadioTransmitterStatus currentStatus = radioTransmitterRecodableComponent.Handler.Status;
            if ((currentStatus != RadioTransmitterStatus.Green) && (currentStatus != RadioTransmitterStatus.Yellow))
            {
                LoggingController.LogWarning("[DSP Not Changed] Aggressor does not have an encoded DSP. DSP Status: " + currentStatus);
                return;
            }

            LoggingController.LogInfo(lastAgressorPlayer.GetText() + " attacked " + player.GetText() + " on Lightkeeper Island. Updating his DSP...");
            changeLightkeeperStanding(lastAgressorPlayer);
        }

        private static void changeLightkeeperStanding(Player player)
        {
            if (!player.Profile.TryGetTraderInfo(lightkeeperTraderId, out Profile.TraderInfo traderInfo))
            {
                LoggingController.LogError("[DSP Not Changed] Could not retrieve Lightkeeper TraderInfo for " + player.GetText());
                return;
            }

            RadioTransmitterRecodableComponent radioTransmitterRecodableComponent = player.FindRadioTransmitter();
            if (radioTransmitterRecodableComponent == null)
            {
                LoggingController.LogError("[DSP Not Changed] Could not retrieve DSP component for " + player.GetText());
                return;
            }

            // From LocalPlayer.OnBeenKilledByAggressor
            if (traderInfo.Standing > 0.009999999776482582)
            {
                traderInfo.SetStanding(0.009999999776482582);
                if (radioTransmitterRecodableComponent != null)
                {
                    radioTransmitterRecodableComponent.SetStatus(RadioTransmitterStatus.Yellow);
                }
            }
            else
            {
                traderInfo.SetStanding(0.0);
                if (radioTransmitterRecodableComponent != null)
                {
                    radioTransmitterRecodableComponent.SetEncoded(false);
                }
            }
        }
    }
}
