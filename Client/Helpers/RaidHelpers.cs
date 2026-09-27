using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using QuestingBots.Patches;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Helpers
{
    public static class RaidHelpers
    {
        public static bool ForcePScavs { get; set; } = false;

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

        public static bool IsDayInGame()
        {
            BotZonesLeaveController? botZonesLeaveController = Singleton<IBotGame>.Instance.BotsController?.ZonesLeaveController;
            if (botZonesLeaveController == null )
            {
                throw new InvalidOperationException("Could not retrieve BotZonesLeaveController");
            }

            DateTime gameDateTime = GetGameDateTime();
            return botZonesLeaveController.IsDayByHour(gameDateTime);
        }

        public static DateTime GetGameDateTime()
        {
            if (Singleton<GameWorld>.Instance?.GameDateTime == null)
            {
                throw new InvalidOperationException("Could not get current game time");
            }

            return Singleton<GameWorld>.Instance.GameDateTime.Calculate();
        }

        public static bool ShouldSpawnPScavByChance()
        {
            if (Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled && Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.PScavs.Enabled)
            {
                return ForcePScavs;
            }

            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.AdjustPScavChance.Enabled)
            {
                return false;
            }

            double[][] chanceVsTimeRemainingFraction = Singleton<ConfigUtil>.Instance.CurrentConfig.AdjustPScavChance.ChanceVsTimeRemainingFraction;
            float remainingRaidTimeFraction = GetRaidTimeRemainingFraction();

            double pScavChance = chanceVsTimeRemainingFraction.InterpolateForFirstCol(remainingRaidTimeFraction);

            System.Random random = new System.Random();
            if (random.NextDouble() * 100 < pScavChance)
            {
                return true;
            }

            return false;
        }
    }
}
