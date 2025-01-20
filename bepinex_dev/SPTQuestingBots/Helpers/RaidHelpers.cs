using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Helpers
{
    public static class RaidHelpers
    {
        public static bool IsBeginningOfRaid() => GetRaidTimeRemainingFraction() > 0.98;
        public static bool HumanPlayersRecentlySpawned() => SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetSecondsSinceSpawning() < 5;

        public static float GetRaidTimeRemainingFraction()
        {
            if (SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                return SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            }

            return (float)SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.RaidTimeRemainingFraction;
        }
    }
}
