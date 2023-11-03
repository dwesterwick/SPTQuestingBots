using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic.Follow
{
    internal class FollowBossAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        public FollowBossAction(BotOwner _BotOwner) : base(_BotOwner)
        {

        }

        public override void Update()
        {
            UpdateBotMovement(CanSprint);

            // Don't allow expensive parts of this behavior (calculating a path to an objective) to run too often
            if (!canUpdate())
            {
                return;
            }

            // Only allow the bot to sprint if its boss is allowed to sprint
            BotOwner boss = HiveMind.BotHiveMindMonitor.GetBoss(BotOwner);
            CanSprint = HiveMind.BotHiveMindMonitor.GetValueForBot(HiveMind.BotHiveMindSensorType.CanSprintToObjective, boss);

            NavMeshPathStatus? pathStatus = RecalculatePath(boss.Position);
        }
    }
}
