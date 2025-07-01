using EFT;
using SPTQuestingBots.BotLogic.ExternalMods;
using SPTQuestingBots.BotLogic.ExternalMods.ModInfo;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.BotMonitor.Monitors
{
    public class BotCombatMonitor : AbstractBotMonitor
    {
        public bool IsInCombat { get; private set; } = false;

        private MinMaxConfig minMaxSearchTimeAfterCombat = ConfigController.Config.Questing.BotQuestingRequirements.SearchTimeAfterCombat.PrioritizedQuesting;
        private double searchTimeAfterCombat = ConfigController.Config.Questing.BotQuestingRequirements.SearchTimeAfterCombat.PrioritizedQuesting.Min;
        private System.Random random = new System.Random();

        public BotCombatMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Start()
        {
            string brainName = BotOwner.Brain.BaseBrain.ShortName();
            minMaxSearchTimeAfterCombat = ExternalModHandler.GetSearchTimeAfterCombat(brainName);
        }

        public override void Update()
        {
            IsInCombat = isInCombat();
        }

        public bool IsSAINLayerActive() => SAINModInfo.IsSAINLayer(BotOwner.GetActiveLayerName());

        private bool isInCombat()
        {
            if (shouldSearchForEnemy(searchTimeAfterCombat))
            {
                if (!IsInCombat)
                {
                    searchTimeAfterCombat = updateSearchTimeAfterCombat();
                    //LoggingController.LogInfo("Bot " + BotOwner.GetText() + " will spend " + searchTimeAfterCombat + " seconds searching for enemies after combat ends..");
                }

                return updateCombatState(true);
            }

            return updateCombatState(false);
        }

        private bool shouldSearchForEnemy(double maxTimeSinceCombatEnded)
        {
            bool hasCloseDanger = BotOwner.Memory.DangerData.HaveCloseDanger;

            bool wasInCombat = (Time.time - BotOwner.Memory.LastTimeHit) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - BotOwner.Memory.EnemySetTime) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - BotOwner.Memory.LastEnemyTimeSeen) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - BotOwner.Memory.UnderFireTime) < maxTimeSinceCombatEnded;

            return wasInCombat || hasCloseDanger;
        }

        private int updateSearchTimeAfterCombat()
        {
            int min = (int)minMaxSearchTimeAfterCombat.Min;
            int max = (int)minMaxSearchTimeAfterCombat.Max;

            return random.Next(min, max);
        }

        private bool updateCombatState(bool inCombat)
        {
            if (inCombat)
            {
                BotMonitor.GetMonitor<BotHealthMonitor>().PauseHealthMonitoring();
            }
            else
            {
                BotMonitor.GetMonitor<BotHealthMonitor>().ResumeHealthMonitoring();
            }

            BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.InCombat, BotOwner, inCombat);

            return inCombat;
        }
    }
}
