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
        private BotObjectiveManager objectiveManager;
        
        public GoToObjectiveAction(BotOwner _BotOwner) : base(_BotOwner)
        {
            objectiveManager = BotOwner.GetPlayer.gameObject.GetComponent<BotObjectiveManager>();
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
            UpdateBotMovement();

            if (!canUpdate())
            {
                return;
            }

            if (!objectiveManager.IsObjectiveActive || !objectiveManager.Position.HasValue)
            {
                return;
            }

            if (!objectiveManager.CanReachObjective)
            {
                return;
            }

            if (!objectiveManager.IsObjectiveReached && objectiveManager.IsCloseToObjective())
            {
                LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " reached its objective (" + objectiveManager + ").");
                objectiveManager.CompleteObjective();
                return;
            }

            tryMoveToObjective();
        }

        private bool tryMoveToObjective()
        {
            NavMeshPathStatus? pathStatus = BotOwner.Mover?.GoToPoint(objectiveManager.Position.Value, true, 0.5f, false, false);

            if (!pathStatus.HasValue || (pathStatus.Value == NavMeshPathStatus.PathInvalid))
            {
                LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " cannot find a path to " + objectiveManager);
                objectiveManager.RejectObjective();
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
                    missingDistance = Vector3.Distance(objectiveManager.Position.Value, lastPathPoint.Value);
                    distanceToEndOfPath = Vector3.Distance(BotOwner.Position, lastPathPoint.Value);
                    distanceToObjective = Vector3.Distance(BotOwner.Position, objectiveManager.Position.Value);
                }

                if (distanceToEndOfPath < ConfigController.Config.BotSearchDistances.MaxNavMeshPathError)
                {
                    if (distanceToObjective < ConfigController.Config.BotSearchDistances.ObjectiveReachedNavMeshPathError)
                    {
                        LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objectiveManager + "). Got close enough. Remaining distance to objective: " + distanceToObjective);
                        objectiveManager.CompleteObjective();
                        return true;
                    }
                    else
                    {
                        LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objectiveManager + "). Giving up. Remaining distance to objective: " + distanceToObjective);
                        objectiveManager.RejectObjective();
                        return false;
                    }
                }

                if (objectiveManager.IsObjectivePathComplete)
                {
                    LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objectiveManager + "). Trying anyway. Distance from end of path to objective: " + missingDistance);
                }

                objectiveManager.IsObjectivePathComplete = false;
            }

            if (!objectiveManager.CanReachObjective && objectiveManager.CanChangeObjective)
            {
                objectiveManager.TryChangeObjective();
            }

            return false;
        }
    }
}
