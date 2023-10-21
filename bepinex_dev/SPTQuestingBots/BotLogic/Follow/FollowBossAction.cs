using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic.Follow
{
    internal class FollowBossAction : GoToPositionAbstractAction
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

            CanSprint = BotHiveMindMonitor.CanBossSprintToObjective(BotOwner);

            BotOwner boss = BotHiveMindMonitor.GetBoss(BotOwner);
            NavMeshPathStatus? pathStatus = RecalculatePath(boss.Position);
        }
    }
}
