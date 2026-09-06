using EFT;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.HiveMind
{
    public class BotHiveMindCanQuestSensor : BotHiveMindAbstractSensor
    {
        public BotHiveMindCanQuestSensor() : base(false)
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
                botState[bot] = objectiveManager.IsQuestingAllowed;
            }
            else
            {
                botState[bot] = defaultValue;
            }
        }
    }
}
