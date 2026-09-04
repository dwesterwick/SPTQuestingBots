using Comfort.Common;
using EFT;
using EFT.Interactive;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.Questing;
using QuestingBots.Utils;
using QuestingBots.Utils.Benchmarking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QuestingBots.Components
{
    public class BotQuestSelector : BehaviorExtensions.MonoBehaviourDelayedUpdate
    {
        private BotOwner _botOwner = null!;
        private ExfiltrationPoint _exfiltrationPoint = null!;
        private BotJobAssignmentCreationJob? assignmentCreationJob = null;

        public bool NewAssignmentReady => assignmentCreationJob?.NewAssignmentReady == true;

        public BotJobAssignment? GetCurrentJobAssignment() => _botOwner.GetMostRecentJobAssignment();

        public static BotQuestSelector GetBotQuestSelector(BotOwner botOwner)
        {
            BotQuestSelector botQuestSelector = botOwner.gameObject.GetOrAddComponent<BotQuestSelector>();
            botQuestSelector.Init(botOwner);

            return botQuestSelector;
        }

        public void Init(BotOwner botOwner)
        {
            _botOwner = botOwner;
            SetExfiltrationPointForQuesting();
        }

        public void SetExfiltrationPointForQuesting()
        {
            Dictionary<ExfiltrationPoint, float> exfiltrationPointDistances = Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints
                .ToDictionary(p => p, p => Vector3.Distance(p.transform.position, _botOwner.Position));

            if (exfiltrationPointDistances.Count > 0)
            {
                KeyValuePair<ExfiltrationPoint, float> furthestPoint = exfiltrationPointDistances
                    .OrderBy(p => p.Value)
                    .Last();

                _exfiltrationPoint = furthestPoint.Key;

                //Singleton<LoggingUtil>.Instance.LogInfo(botOwner.GetText() + " has selected " + furthestPoint.Key.Settings.Name + " as its furthest exfil point (" + furthestPoint.Value + "m)");
            }
        }

        public void RefreshExfiltrationPointForQuesting()
        {
            BotObjectiveManager? objectiveManager = _botOwner.GetObjectiveManager();
            if (objectiveManager == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot retrieve BotObjectiveManager for " + _botOwner.GetText());
                return;
            }

            float maxDistanceBetweenExfils = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().GetMaxDistanceBetweenExfils();
            float minDistanceToSwitchExfil = maxDistanceBetweenExfils * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilReachedMinFraction;

            // If the bot is close to its selected exfil (only used for quest selection), select a new one
            float? distanceToExfilPoint = DistanceToExfiltrationPointForQuesting();
            if (distanceToExfilPoint.HasValue && (distanceToExfilPoint.Value < minDistanceToSwitchExfil))
            {
                objectiveManager.QuestSelector.SetExfiltrationPointForQuesting();
            }
        }

        public float? DistanceToExfiltrationPointForQuesting()
        {
            if (_exfiltrationPoint == null)
            {
                return null;
            }

            return Vector3.Distance(_botOwner.Position, _exfiltrationPoint.transform.position);
        }

        public Vector3? VectorToExfiltrationPointForQuesting()
        {
            if (_exfiltrationPoint == null)
            {
                return null;
            }

            return _exfiltrationPoint.transform.position - _botOwner.Position;
        }

        public void RefreshJobAssignment()
        {
            if (HasActiveAssignment(out _))
            {
                return;
            }

            if (TryAssignNextObjectiveStep())
            {
                return;
            }

            if (IsNewJobCreationJobRunning())
            {
                return;
            }

            TryCreateNewJobAssignment();
        }

        public bool HasActiveAssignment(out BotJobAssignment? currentAssignment)
        {
            currentAssignment = _botOwner.GetMostRecentJobAssignment();
            if (currentAssignment == null)
            {
                return false;
            }

            return currentAssignment.IsActive;
        }

        public bool TryAssignNextObjectiveStep()
        {
            if (HasActiveAssignment(out BotJobAssignment? currentAssignment))
            {
                return false;
            }

            // Check if more steps are available for the bot's current assignment
            if ((currentAssignment != null) && currentAssignment.TrySetNextObjectiveStep(false))
            {
                assignmentCreationJob = null;
                return true;
            }

            //Singleton<LoggingUtil>.Instance.LogInfo("There are no more steps available for " + bot.GetText() + " in " + (currentAssignment.QuestObjectiveAssignment?.ToString() ?? "???"));
            return false;
        }

        public bool IsNewJobCreationJobRunning()
        {
            if (assignmentCreationJob == null)
            {
                return false;
            }

            if (assignmentCreationJob.IsCreatingAnAssignment)
            {
                //Singleton<LoggingUtil>.Instance.LogDebug("Waiting for an assignment to be created for " + _botOwner.GetText());
                return true;
            }

            return false;
        }

        public bool TryCreateNewJobAssignment()
        {
            if (assignmentCreationJob?.IsCreatingAnAssignment == true)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Discarding pending job assignment creation task for " + _botOwner.GetText());
            }

            if (assignmentCreationJob?.NewAssignmentReady == true)
            {
                //Singleton<LoggingUtil>.Instance.LogDebug("Discarding finished job assignment creation task for " + _botOwner.GetText());
            }

            BotObjectiveManager? objectiveManager = _botOwner.GetObjectiveManager();
            if (objectiveManager == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot retrieve BotObjectiveManager for " + _botOwner.GetText());
                return false;
            }

            if (!objectiveManager.IsQuestingAllowed)
            {
                return false;
            }

            // Do not select another quest objective if the bot wants to extract
            if (objectiveManager.DoesBotWantToExtract())
            {
                return false;
            }

            // If the bot is close to its originally selected exfiltration point, choose a new one to keep it moving around the map
            RefreshExfiltrationPointForQuesting();

            assignmentCreationJob = new BotJobAssignmentCreationJob(_botOwner);
            StartCoroutine(assignmentCreationJob.CreateNewBotJobAssignment());

            return true;
        }
    }
}
