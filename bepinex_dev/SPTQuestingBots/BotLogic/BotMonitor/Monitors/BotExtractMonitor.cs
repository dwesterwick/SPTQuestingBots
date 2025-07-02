using EFT;
using SPTQuestingBots.BotLogic.ExternalMods;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Extract;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.BotMonitor.Monitors
{
    public class BotExtractMonitor : AbstractBotMonitor
    {
        public bool IsTryingToExtract { get; private set; } = false;
        
        private bool _isBotReadyToExtract = false;
        public bool IsBotReadyToExtract
        {
            get
            {
                if (_isBotReadyToExtract)
                {
                    return true;
                }

                _isBotReadyToExtract = isBotReadyToExtract();
                return _isBotReadyToExtract;
            }
        }

        private AbstractExtractFunction extractFunction;
        float maxQuestsScalingFactor = 1;
        private int minTotalQuestsForExtract = int.MaxValue;
        private int minEFTQuestsForExtract = int.MaxValue;
        private System.Random random;

        public BotExtractMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Start()
        {
            random = new System.Random();

            if (BotGenerator.TryGetBotGroupFromAnyGenerator(BotOwner, out BotSpawnInfo botGroup))
            {
                maxQuestsScalingFactor = botGroup.IsInitialSpawn ? RaidHelpers.InitialRaidTimeFraction : 1;
            }

            extractFunction = ExternalModHandler.CreateExtractFunction(BotOwner);
        }

        public override void Update()
        {
            IsTryingToExtract = extractFunction.IsTryingToExtract();
        }

        public bool TryInstructBotToExtract() => extractFunction.TryInstructBotToExtract();

        private bool isBotReadyToExtract()
        {
            // Prevent the bot from extracting too soon after it spawns
            if (Time.time - BotOwner.ActivateTime < ConfigController.Config.Questing.ExtractionRequirements.MinAliveTime)
            {
                return false;
            }

            // If the raid is about to end, make the bot extract
            float remainingRaidTime = RaidHelpers.GetRemainingRaidTimeSeconds();
            if (remainingRaidTime < ConfigController.Config.Questing.ExtractionRequirements.MustExtractTimeRemaining)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " is ready to extract because the raid will be over in " + remainingRaidTime + " seconds.");
                return true;
            }

            // Ensure enough time has elapsed in the raid to prevent players from getting run-throughs
            if (!RaidHelpers.MinimumSurvivalTimeExceeded())
            {
                return false;
            }

            if (hasCompletedEnoughTotalQuests(maxQuestsScalingFactor))
            {
                return true;
            }

            if (hasCompletedEnoughEFTQuests(maxQuestsScalingFactor))
            {
                return true;
            }

            return false;
        }

        private bool hasCompletedEnoughTotalQuests(float scalingFactor)
        {
            // Select a random number of total quests the bot must complete before it's allowed to extract
            if (minTotalQuestsForExtract == int.MaxValue)
            {
                Configuration.MinMaxConfig minMax = ConfigController.Config.Questing.ExtractionRequirements.TotalQuests * scalingFactor;
                minTotalQuestsForExtract = random.Next((int)minMax.Min, (int)minMax.Max);
            }

            // Check if the bot has completed enough total quests to extract
            int totalQuestsCompleted = BotOwner.NumberOfCompletedOrAchivedQuests();
            if (totalQuestsCompleted >= minTotalQuestsForExtract)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " has completed " + totalQuestsCompleted + " quests and is ready to extract.");
                return true;
            }
            //LoggingController.LogInfo(botOwner.GetText() + " has completed " + totalQuestsCompleted + "/" + minTotalQuestsForExtract + " quests");

            return false;
        }

        private bool hasCompletedEnoughEFTQuests(float scalingFactor)
        {
            // Select a random number of EFT quests the bot must complete before it's allowed to extract
            if (minEFTQuestsForExtract == int.MaxValue)
            {
                Configuration.MinMaxConfig minMax = ConfigController.Config.Questing.ExtractionRequirements.EFTQuests * scalingFactor;
                minEFTQuestsForExtract = random.Next((int)minMax.Min, (int)minMax.Max);
            }

            // Check if the bot has completed enough EFT quests to extract
            int EFTQuestsCompleted = BotOwner.NumberOfCompletedOrAchivedEFTQuests();
            if (EFTQuestsCompleted >= minEFTQuestsForExtract)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " has completed " + EFTQuestsCompleted + " EFT quests and is ready to extract.");
                return true;
            }
            //LoggingController.LogInfo(botOwner.GetText() + " has completed " + EFTQuestsCompleted + "/" + minEFTQuestsForExtract + " EFT quests");

            return false;
        }
    }
}
