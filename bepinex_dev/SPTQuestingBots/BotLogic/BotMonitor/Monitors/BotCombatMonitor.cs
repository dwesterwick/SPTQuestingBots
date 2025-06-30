using EFT;
using SPTQuestingBots.BotLogic.ExternalMods;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Controllers;
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

        private double searchTimeAfterCombat = ConfigController.Config.Questing.BotQuestingRequirements.SearchTimeAfterCombat.PrioritizedQuesting.Min;

        public BotCombatMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Update()
        {
            IsInCombat = isInCombat();
        }

        private bool isInCombat()
        {
            if (shouldSearchForEnemy(searchTimeAfterCombat))
            {
                if (!BotHiveMindMonitor.GetValueForBot(BotHiveMindSensorType.InCombat, BotOwner))
                {
                    /*bool hasTarget = BotOwner.Memory.GoalTarget.HaveMainTarget();
                    if (hasTarget)
                    {
                        string message = "Bot " + BotOwner.GetText() + " is in combat.";
                        message += " Close danger: " + BotOwner.Memory.DangerData.HaveCloseDanger + ".";
                        message += " Last Time Hit: " + BotOwner.Memory.LastTimeHit + ".";
                        message += " Enemy Set Time: " + BotOwner.Memory.EnemySetTime + ".";
                        message += " Last Enemy Seen Time: " + BotOwner.Memory.LastEnemyTimeSeen + ".";
                        message += " Under Fire Time: " + BotOwner.Memory.UnderFireTime + ".";
                        LoggingController.LogInfo(message);
                    }*/

                    searchTimeAfterCombat = updateSearchTimeAfterCombat();
                    //LoggingController.LogInfo("Bot " + BotOwner.GetText() + " will spend " + searchTimeAfterCombat + " seconds searching for enemies after combat ends..");
                }

                BotMonitor.GetMonitor<BotHealthMonitor>().PauseHealthMonitoring();

                BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.InCombat, BotOwner, true);
                return true;
            }

            BotMonitor.GetMonitor<BotHealthMonitor>().ResumeHealthMonitoring();

            BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.InCombat, BotOwner, false);
            return false;
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
            string brainName = BotOwner.Brain.BaseBrain.ShortName();
            MinMaxConfig minMax = ExternalModHandler.GetSearchTimeAfterCombat(brainName);

            System.Random random = new System.Random();
            int min = (int)minMax.Min;
            int max = (int)minMax.Max;

            return random.Next(min, max);
        }
    }
}
