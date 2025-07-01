using EFT;
using SPTQuestingBots.BotLogic.ExternalMods;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Loot;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.BotMonitor.Monitors
{
    public class BotLootingMonitor : AbstractBotMonitor
    {
        public bool IsLooting { get; private set; } = false;
        public bool IsSearchingForLoot { get; private set; } = false;
        public float NextLootCheckDelay { get; private set; } = ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingChecks;

        private AbstractLootFunction lootFunction;
        private Stopwatch lootSearchTimer = new Stopwatch();
        private bool wasLooting = false;
        private bool hasFoundLoot = false;

        public TimeSpan TimeSinceBossLastLooted => DateTime.Now - BotHiveMindMonitor.GetLastLootingTimeForBoss(BotOwner);
        public bool BossWillAllowLootingByTime => TimeSinceBossLastLooted.TotalSeconds > ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenFollowerLootingChecks;
        public bool BossWillAllowLootingByDistance => BotHiveMindMonitor.GetDistanceToBoss(BotOwner) <= ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MaxDistanceFromBoss;
        public bool BossWillAllowLooting => (BossWillAllowLootingByTime && ShouldCheckForLoot()) || (BossWillAllowLootingByDistance && IsLooting);

        public BotLootingMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Start()
        {
            lootFunction = ExternalModHandler.CreateLootFunction(BotOwner);
        }

        public override void Update()
        {
            IsLooting = lootFunction.IsLooting();
            IsSearchingForLoot = lootFunction.IsSearchingForLoot();
        }

        public bool TryPreventBotFromLooting(float duration) => lootFunction.TryPreventBotFromLooting(duration);
        public bool TryForceBotToScanLoot() => lootFunction.TryForceBotToScanLoot();

        public bool ShouldCheckForLoot() => ShouldCheckForLoot(NextLootCheckDelay);
        public bool ShouldCheckForLoot(float minTimeBetweenLooting)
        {
            if (!ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.Enabled)
            {
                return false;
            }

            // Check if LootingBots is loaded
            if (!lootFunction.CanMonitoredLayerBeUsed)
            {
                return false;
            }

            NextLootCheckDelay = ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingChecks;

            // The following logic is used to determine if a bot is allowed to search for loot:
            //      - If LootingBots has instructed the bot to check a lootable container, allow it
            //      - If the bot hasn't serached for loot for a minimum amount of time, allow it
            //      - After the minimum amount of time, the bot will only be allowed to search for a certain amount of time. If it doesn't find any loot
            //        in that time, it will be forced to continue questing
            //      - The minimum amount of time between loot checks depends on whether the bot successfully found loot during the previous check
            if
            (
                (IsLooting || (lootSearchTimer.ElapsedMilliseconds < 1000 * ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MaxLootScanTime))
                && (IsLooting || IsSearchingForLoot || lootFunction.CanUseMonitoredLayer(minTimeBetweenLooting))
            )
            {
                if (IsLooting)
                {
                    if (!hasFoundLoot)
                    {
                        LoggingController.LogDebug("Bot " + BotOwner.GetText() + " has found loot");
                    }

                    NextLootCheckDelay = ConfigController.Config.Questing.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingEvents;
                    lootSearchTimer.Reset();
                    hasFoundLoot = true;
                }
                else
                {
                    if (!wasLooting)
                    {
                        //LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is searching for loot...");
                    }

                    lootSearchTimer.Start();
                }

                if (IsSearchingForLoot || IsLooting)
                {
                    wasLooting = true;
                }

                lootFunction.ResetMonitoredLayerCanUseTimer();
                return true;
            }

            if (wasLooting || hasFoundLoot)
            {
                lootFunction.ResetMonitoredLayerCanUseTimer();
                //LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is done looting (Loot searching time: " + (lootSearchTimer.ElapsedMilliseconds / 1000.0) + ").");
            }

            lootSearchTimer.Reset();
            wasLooting = false;
            hasFoundLoot = false;
            return false;
        }
    }
}
