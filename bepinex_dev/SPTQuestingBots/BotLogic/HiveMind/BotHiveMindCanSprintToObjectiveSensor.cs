using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.HiveMind
{
    public class BotHiveMindCanSprintToObjectiveSensor: BotHiveMindAbstractSensor
    {
        public BotHiveMindCanSprintToObjectiveSensor() : base(true)
        {

        }

        public override void Update(Action<BotOwner> additionalAction = null)
        {
            Action<BotOwner> updateFromObjectiveManager = new Action<BotOwner>((bot) =>
            {
                if (bot?.GetPlayer?.gameObject?.TryGetComponent(out Objective.BotObjectiveManager objectiveManager) == true)
                {
                    botState[bot] = objectiveManager?.CanSprintToObjective() ?? defaultValue;
                }
            });

            base.Update(updateFromObjectiveManager);
        }
    }
}
