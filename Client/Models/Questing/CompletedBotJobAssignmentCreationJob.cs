using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.Questing
{
    public class CompletedBotJobAssignmentCreationJob : IBotJobAssignmentCreationJob
    {
        public bool IsCreatingAnAssignment => false;
        public bool NewAssignmentReady => true;

        private BotJobAssignment? _assignmentCreationResult;
        public BotJobAssignment? AssignmentCreationResult => _assignmentCreationResult;

        public CompletedBotJobAssignmentCreationJob(BotJobAssignment? botJobAssignment)
        {
            _assignmentCreationResult = botJobAssignment;
        }

        public IEnumerator CreateNewBotJobAssignment()
        {
            yield break;
        }
    }
}
