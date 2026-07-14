using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.ExternalMods;
using QuestingBots.BotLogic.ExternalMods.Functions.Loot;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.BotMonitor.Monitors
{
    public class BotLootingMonitor : AbstractBotMonitor
    {
        public bool IsLooting { get; private set; } = false;
        public bool IsSearchingForLoot { get; private set; } = false;
        public float NextLootCheckDelay { get; private set; } = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingChecks;

        private AbstractLootFunction lootFunction = null!;
        private Stopwatch lootSearchTimer = new Stopwatch();
        private Stopwatch forcedLootSearchTimer = new Stopwatch();
        private bool wasLooting = false;
        private bool hasFoundLoot = false;

        public TimeSpan TimeSinceBossLastLooted => DateTime.Now - BotHiveMindMonitor.GetLastLootingTimeForBoss(BotOwner);
        public bool BossWillAllowLootingByTime => TimeSinceBossLastLooted.TotalSeconds > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenFollowerLootingChecks;
        public bool BossWillAllowLootingByDistance => BotHiveMindMonitor.GetDistanceToBoss(BotOwner) <= Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.BreakForLooting.MaxDistanceFromBoss;
        public bool BossWillAllowLooting => (BossWillAllowLootingByTime && ShouldCheckForLoot()) || (BossWillAllowLootingByDistance && IsLooting);
        public bool IsForcedToSearchForLoot => forcedLootSearchTimer.IsRunning && (forcedLootSearchTimer.ElapsedMilliseconds < 1000 * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.BreakForLooting.MaxLootScanTime);

        public BotLootingMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Start()
        {
            lootFunction = ExternalModHandler.CreateLootFunction(BotOwner);
        }

        public override void UpdateIfQuesting()
        {
            IsLooting = lootFunction.IsLooting();
            IsSearchingForLoot = lootFunction.IsSearchingForLoot();
        }

        public bool TryPreventBotFromLooting(float duration) => lootFunction.TryPreventBotFromLooting(duration);

        public bool TryForceBotToScanLoot()
        {
            forcedLootSearchTimer.Restart();

            return lootFunction.TryForceBotToScanLoot();
        }

        public bool ShouldCheckForLoot() => ShouldCheckForLoot(NextLootCheckDelay);
        public bool ShouldCheckForLoot(float minTimeBetweenLooting)
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.BreakForLooting.Enabled)
            {
                return false;
            }

            // Check if LootingBots is loaded
            if (!lootFunction.CanMonitoredLayerBeUsed)
            {
                return false;
            }

            NextLootCheckDelay = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingChecks;

            if (!allowedToCheckForLoot(minTimeBetweenLooting))
            {
                if (wasLooting || hasFoundLoot)
                {
                    lootFunction.ResetMonitoredLayerCanUseTimer();
                    //Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " is done looting (Loot searching time: " + (lootSearchTimer.ElapsedMilliseconds / 1000.0) + ").");
                }

                lootSearchTimer.Reset();
                wasLooting = false;
                hasFoundLoot = false;
                return false;
            }

            if (IsLooting)
            {
                if (!hasFoundLoot)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " has found loot");
                }

                NextLootCheckDelay = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingEvents;
                lootSearchTimer.Reset();
                hasFoundLoot = true;
                wasLooting = true;
            }
            else
            {
                if (!wasLooting)
                {
                    //Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " is searching for loot...");
                }

                if (IsSearchingForLoot)
                {
                    wasLooting = true;
                }

                lootSearchTimer.Start();
            }

            lootFunction.ResetMonitoredLayerCanUseTimer();
            return true;
        }

        private bool allowedToCheckForLoot(float minTimeBetweenLooting)
        {
            // Check if the bot is already trying to loot something
            if (IsLooting)
            {
                return true;
            }

            // Check if the bot has spent too much time scanning for loot
            bool maxLootScanTimeExceeded = lootSearchTimer.ElapsedMilliseconds > 1000 * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.BreakForLooting.MaxLootScanTime;
            if (maxLootScanTimeExceeded)
            {
                return false;
            }

            // Check if the bot is currently scanning for loot
            if (IsSearchingForLoot)
            {
                return true;
            }
            
            // Check if enough time has elapsed after the previous time the bot scanned for loot
            bool allowedToLootByTime = lootFunction.CanUseMonitoredLayer(minTimeBetweenLooting);
            if (allowedToLootByTime)
            {
                return true;
            }

            return false;
        }
    }
}
