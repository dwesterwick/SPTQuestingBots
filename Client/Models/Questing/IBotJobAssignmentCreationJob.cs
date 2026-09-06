using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.Questing
{
    public interface IBotJobAssignmentCreationJob
    {
        bool IsCreatingAnAssignment { get; }
        bool NewAssignmentReady { get; }
        BotJobAssignment? AssignmentCreationResult { get; }

        IEnumerator CreateNewBotJobAssignment();
    }
}
