using EFT;
using SPTQuestingBots.BotLogic.ExternalMods;
using SPTQuestingBots.Configuration;
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
        public BotCombatMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Update()
        {
            
        }

        public bool ShouldSearchForEnemy(double maxTimeSinceCombatEnded)
        {
            bool hasCloseDanger = BotOwner.Memory.DangerData.HaveCloseDanger;

            bool wasInCombat = (Time.time - BotOwner.Memory.LastTimeHit) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - BotOwner.Memory.EnemySetTime) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - BotOwner.Memory.LastEnemyTimeSeen) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - BotOwner.Memory.UnderFireTime) < maxTimeSinceCombatEnded;

            return wasInCombat || hasCloseDanger;
        }

        public int UpdateSearchTimeAfterCombat()
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
