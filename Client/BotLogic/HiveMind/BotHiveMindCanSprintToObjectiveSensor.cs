using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using QuestingBots.Controllers;

namespace QuestingBots.BotLogic.HiveMind
{
    public class BotHiveMindCanSprintToObjectiveSensor: BotHiveMindAbstractSensor
    {
        public BotHiveMindCanSprintToObjectiveSensor() : base(true)
        {

        }

        public override void Update(Action<BotOwner>? additionalAction = null)
        {
            base.Update(updateBotState);
        }

        private void updateBotState(BotOwner bot)
        {
            Components.BotObjectiveManager? objectiveManager = bot.GetObjectiveManager();
            if (objectiveManager != null)
            {
                botState[bot] = objectiveManager.CanSprintToObjective();
            }
            else
            {
                botState[bot] = defaultValue;
            }
        }
    }
}
