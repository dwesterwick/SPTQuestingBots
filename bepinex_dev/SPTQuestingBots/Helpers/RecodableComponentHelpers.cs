using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.InventoryLogic;

namespace SPTQuestingBots.Helpers
{
    public static class RecodableComponentHelpers
    {
        private static string lightkeeperTraderId = "638f541a29ffd1183d187f57";

        public static bool HasAGreenOrYellowDSP(this Player player)
        {
            RadioTransmitterRecodableComponent radioTransmitterRecodableComponent = player.FindRadioTransmitter();
            if (radioTransmitterRecodableComponent == null)
            {
                return false;
            }

            RadioTransmitterStatus currentStatus = radioTransmitterRecodableComponent.Handler.Status;
            if ((currentStatus == RadioTransmitterStatus.Green) || (currentStatus == RadioTransmitterStatus.Yellow))
            {
                return true;
            }

            return false;
        }

        public static bool TryReduceLightkeeperStanding(this Player player)
        {
            if (!player.Profile.TryGetTraderInfo(lightkeeperTraderId, out Profile.TraderInfo traderInfo))
            {
                return false;
            }

            RadioTransmitterRecodableComponent radioTransmitterRecodableComponent = player.FindRadioTransmitter();
            if (radioTransmitterRecodableComponent == null)
            {
                return false;
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

            return true;
        }
    }
}
