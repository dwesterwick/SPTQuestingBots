using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic
{
    internal class GoToObjectiveAction : CustomLogicDelayedUpdate
    {
        private BotObjectiveManager objective;
        
        public GoToObjectiveAction(BotOwner _BotOwner) : base(_BotOwner)
        {
            objective = BotOwner.GetPlayer.gameObject.GetComponent<BotObjectiveManager>();
        }

        public override void Start()
        {
            BotOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            BotOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            if (!canUpdate())
            {
                return;
            }

            if (!objective.IsObjectiveActive || !objective.Position.HasValue)
            {
                return;
            }

            if (!objective.CanReachObjective)
            {
                return;
            }

            if (!objective.IsObjectiveReached && objective.IsCloseToObjective())
            {
                LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " reached its objective (" + objective + ").");
                objective.CompleteObjective();
                return;
            }

            tryMoveToObjective();
        }

        private bool tryMoveToObjective()
        {
            NavMeshPathStatus? pathStatus = BotOwner.Mover?.GoToPoint(objective.Position.Value, true, 0.5f, false, false);

            if (!pathStatus.HasValue || (pathStatus.Value == NavMeshPathStatus.PathInvalid))
            {
                LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " cannot find a path to " + objective);
                objective.RejectObjective();
                return false;
            }

            if (pathStatus.HasValue && (pathStatus.Value == NavMeshPathStatus.PathPartial))
            {
                Vector3? lastPathPoint = BotOwner.Mover?.CurPathLastPoint;

                float missingDistance = float.NaN;
                float distanceToEndOfPath = float.NaN;
                float distanceToObjective = float.NaN;
                if (lastPathPoint.HasValue)
                {
                    missingDistance = Vector3.Distance(objective.Position.Value, lastPathPoint.Value);
                    distanceToEndOfPath = Vector3.Distance(BotOwner.Position, lastPathPoint.Value);
                    distanceToObjective = Vector3.Distance(BotOwner.Position, objective.Position.Value);
                }

                if (distanceToEndOfPath < ConfigController.Config.BotSearchDistances.MaxNavMeshPathError)
                {
                    if (distanceToObjective < ConfigController.Config.BotSearchDistances.ObjectiveReachedNavMeshPathError)
                    {
                        LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objective + "). Got close enough. Remaining distance to objective: " + distanceToObjective);
                        objective.CompleteObjective();
                        return true;
                    }
                    else
                    {
                        LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objective + "). Giving up. Remaining distance to objective: " + distanceToObjective);
                        objective.RejectObjective();
                        return false;
                    }
                }

                if (objective.IsObjectivePathComplete)
                {
                    LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objective + "). Trying anyway. Distance from end of path to objective: " + missingDistance);
                }

                objective.IsObjectivePathComplete = false;
            }

            if (!objective.CanReachObjective && objective.CanChangeObjective)
            {
                objective.TryChangeObjective();
            }

            return false;
        }
    }
}
