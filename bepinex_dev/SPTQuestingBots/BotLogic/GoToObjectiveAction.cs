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
        private bool canSprint = true;
        
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
            UpdateBotMovement(canSprint);

            // Don't allow expensive parts of this behavior (calculating a path to an objective) to run too often
            if (!canUpdate())
            {
                return;
            }

            // This doesn't really need to be updated every frame
            canSprint = objectiveManager.CanSprintToObjective();

            if (!objectiveManager.IsObjectiveActive || !objectiveManager.Position.HasValue)
            {
                return;
            }

            if (!objectiveManager.CanReachObjective)
            {
                return;
            }

            // Check if the bot just completed its objective
            if (!objectiveManager.IsObjectiveReached && objectiveManager.IsCloseToObjective())
            {
                LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " reached its objective (" + objectiveManager + ").");
                objectiveManager.CompleteObjective();
                return;
            }

            // Recalculate a path to the bot's objective. This should be done cyclically in case locked doors are opened, etc. 
            tryMoveToObjective();
        }

        private bool tryMoveToObjective()
        {
            NavMeshPathStatus? pathStatus = BotOwner.Mover?.GoToPoint(objectiveManager.Position.Value, true, 0.5f, false, false);

            // If the path is invalid, there's nowhere for the bot to move
            if (!pathStatus.HasValue || (pathStatus.Value == NavMeshPathStatus.PathInvalid))
            {
                LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " cannot find a path to " + objectiveManager);
                objectiveManager.RejectObjective();
                return false;
            }

            if (pathStatus.HasValue && (pathStatus.Value == NavMeshPathStatus.PathPartial))
            {
                float distanceToEndOfPath = float.NaN;
                float distanceToObjective = float.NaN;
                float missingDistance = float.NaN;

                // Calculate distances between:
                //  - the bot and the end of the incomplete path to its objective
                //  - the bot and its objective step position
                //  - the end of its partial path and its objective step position
                Vector3? lastPathPoint = BotOwner.Mover?.CurPathLastPoint;
                if (lastPathPoint.HasValue)
                {
                    distanceToEndOfPath = Vector3.Distance(BotOwner.Position, lastPathPoint.Value);
                    distanceToObjective = Vector3.Distance(BotOwner.Position, objectiveManager.Position.Value);
                    missingDistance = Vector3.Distance(objectiveManager.Position.Value, lastPathPoint.Value);
                }

                // Check if the bot is nearly at the end of its (incomplete) path
                if (distanceToEndOfPath < ConfigController.Config.BotSearchDistances.MaxNavMeshPathError)
                {
                    if (distanceToObjective < ConfigController.Config.BotSearchDistances.ObjectiveReachedNavMeshPathError)
                    {
                        LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objectiveManager + "). Got close enough. Remaining distance to objective: " + distanceToObjective);
                        objectiveManager.CompleteObjective();
                        return true;
                    }

                    LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objectiveManager + "). Giving up. Remaining distance to objective: " + distanceToObjective);
                    objectiveManager.RejectObjective();
                    return false;
                }

                // Check if this is the first time an incomplete path was generated. If so, write a warning message. 
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
