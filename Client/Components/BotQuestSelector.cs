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
                Singleton<LoggingUtil>.Instance.LogDebug(_botOwner.GetText() + " is waiting for a new job assignment to be created");
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
                Singleton<LoggingUtil>.Instance.LogDebug("Waiting for an assignment to be created for " + _botOwner.GetText());
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

            assignmentCreationJob = new BotJobAssignmentCreationJob(_botOwner, exfiltrationPoint);
            StartCoroutine(assignmentCreationJob.CreateNewBotJobAssignment());

            return true;
        }
    }
}
