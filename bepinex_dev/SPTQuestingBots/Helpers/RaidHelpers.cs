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
        public static float GetRemainingRaidTimeSeconds() => SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRemainingRaidSeconds();
        public static float GetRaidTimeRemainingFraction() => SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
        public static float GetRaidElapsedSeconds() => SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();
        public static float GetSecondsSinceSpawning() => SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetSecondsSinceSpawning();

        public static bool IsBeginningOfRaid() => GetRaidTimeRemainingFraction() > 0.98;
        public static bool MinimumSurvivalTimeExceeded() => GetRaidElapsedSeconds() >= MinimumSurvivalTime;
        public static bool HumanPlayersRecentlySpawned() => GetSecondsSinceSpawning() < 5;
    }
}
