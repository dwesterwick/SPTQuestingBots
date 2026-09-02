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
        private ExfiltrationPoint exfiltrationPoint = null!;
        private BotJobAssignmentCreationJob? assignmentCreationJob = null;

        public bool NewAssignmentReady => assignmentCreationJob?.NewAssignmentReady == true;

        public static BotQuestSelector GetBotQuestSelector(BotOwner botOwner)
        {
            BotQuestSelector botQuestSelector = botOwner.gameObject.GetOrAddComponent<BotQuestSelector>();
            botQuestSelector.Init(botOwner);

            return botQuestSelector;
        }

        public void Init(BotOwner botOwner)
        {
            _botOwner = botOwner;
            SetExfiliationPointForQuesting();
        }

        [Benchmark]
        public void SetExfiliationPointForQuesting()
        {
            Dictionary<ExfiltrationPoint, float> exfiltrationPointDistances = Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints
                .ToDictionary(p => p, p => Vector3.Distance(p.transform.position, _botOwner.Position));

            if (exfiltrationPointDistances.Count > 0)
            {
                KeyValuePair<ExfiltrationPoint, float> furthestPoint = exfiltrationPointDistances
                    .OrderBy(p => p.Value)
                    .Last();

                exfiltrationPoint = furthestPoint.Key;

                //Singleton<LoggingUtil>.Instance.LogInfo(botOwner.GetText() + " has selected " + furthestPoint.Key.Settings.Name + " as its furthest exfil point (" + furthestPoint.Value + "m)");
            }
        }

        public BotJobAssignment? GetCurrentJobAssignment(bool allowUpdate = true)
        {
            bool hasNewJobAssignment = allowUpdate && DoesBotHaveNewJobAssignment();

            BotJobAssignment? mostRecentAssignment = _botOwner.GetMostRecentJobAssignment();
            if (!hasNewJobAssignment)
            {
                return mostRecentAssignment;
            }
            
            if (assignmentCreationJob != null)
            {
                return assignmentCreationJob.AssignmentCreationResult;
            }

            return mostRecentAssignment;
        }

        public bool DoesBotHaveNewJobAssignment()
        {
            BotJobAssignment? mostRecentAssignment = _botOwner.GetMostRecentJobAssignment();
            if (mostRecentAssignment != null)
            {
                // Check if the bot is currently doing an assignment
                if (mostRecentAssignment.IsActive)
                {
                    return false;
                }

                // Check if more steps are available for the bot's current assignment
                if (mostRecentAssignment.TrySetNextObjectiveStep(false))
                {
                    assignmentCreationJob = null;
                    return true;
                }

                //Singleton<LoggingUtil>.Instance.LogInfo("There are no more steps available for " + bot.GetText() + " in " + (currentAssignment.QuestObjectiveAssignment?.ToString() ?? "???"));
            }

            return NewJobCreationJobRunning();
        }

        [Benchmark]
        public bool NewJobCreationJobRunning()
        {
            if (assignmentCreationJob == null)
            {
                return TryCreateNewJobAssignment();
            }

            if (assignmentCreationJob.IsCreatingAnAssignment)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Waiting for an assignment to be created for " + _botOwner.GetText());
                return true;
            }

            if (!assignmentCreationJob.NewAssignmentReady)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Could not create a new assignment for " + _botOwner.GetText());
                assignmentCreationJob = null;
                return false;
            }

            if (assignmentCreationJob.AssignmentCreationResult == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Created a null job assignment for " + _botOwner.GetText());
                assignmentCreationJob = null;
                return false;
            }

            return true;
        }

        public bool TryCreateNewJobAssignment()
        {
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

            assignmentCreationJob = new BotJobAssignmentCreationJob(_botOwner, exfiltrationPoint);
            StartCoroutine(assignmentCreationJob.CreateNewBotJobAssignment());

            return true;
        }
    }
}
