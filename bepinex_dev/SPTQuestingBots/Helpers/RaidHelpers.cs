using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;

namespace SPTQuestingBots.Helpers
{
    public static class RaidHelpers
    {
        public static bool IsScavRun => SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid;
        public static float OriginalEscapeTimeSeconds => SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds;
        public static float InitialRaidTimeFraction => SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.RaidTimeRemainingFraction;
        public static float MinimumSurvivalTime => SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.NewSurvivalTimeSeconds;

        public static bool IsHostRaid() => Singleton<IBotGame>.Instantiated && Singleton<IBotGame>.Instance.BotsController?.IsEnable == true;
        public static bool HasRaidStarted() => SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted();
        public static bool IsBeginningOfRaid() => GetRaidTimeRemainingFraction() > 0.98;
        public static bool MinimumSurvivalTimeExceeded() => GetRaidElapsedSeconds() >= MinimumSurvivalTime;
        public static bool HumanPlayersRecentlySpawned() => GetSecondsSinceSpawning() < 5;
        public static float GetSecondsSinceSpawning() => SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetSecondsSinceSpawning();

        public static float GetRemainingRaidTimeSeconds()
        {
            // Band-aid fix until SPT is fixed;
            return SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds - 3;

            return SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
        }

        public static float GetRaidTimeRemainingFraction()
        {
            // Band-aid fix until SPT is fixed;
            return 0.99f;

            if (HasRaidStarted())
            {
                return SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            }

            return InitialRaidTimeFraction;
        }

        public static float GetRaidElapsedSeconds()
        {
            // Band-aid fix until SPT is fixed;
            return SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds() - SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds;

            return SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();
        }
    }
}
