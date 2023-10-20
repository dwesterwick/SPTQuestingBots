using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic
{
    internal class FollowBossAction : GoToPositionAbstractAction
    {
        private BotOwner boss = null;
        private BotObjectiveManager objectiveManagerForBoss = null;

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

            if (boss == null)
            {
                boss = BotOwner.BotFollower.BossToFollow?.Player()?.AIData?.BotOwner;
                objectiveManagerForBoss = boss?.GetPlayer?.gameObject?.GetComponent<BotObjectiveManager>();

                return;
            }

            CanSprint = objectiveManagerForBoss.CanSprintToObjective();
            NavMeshPathStatus? pathStatus = RecalculatePath(boss.Position);
        }
    }
}
