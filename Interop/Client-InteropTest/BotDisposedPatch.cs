using EFT;
using QuestingBots;
using QuestingBotsInteropTest;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots_InteropTest
{
    public class BotDisposedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod(nameof(BotOwner.Dispose), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotOwner __instance)
        {
            LoggingController.LogInfo("----------------------------------");
            LoggingController.LogInfo($"Job history entry for {__instance.name}:");

            IEnumerable<QuestingBotsBotJobAssignmentHistoryEntry> allHistoryEntries = QuestingBots.QuestingBotsInterop.GetJobAssignmentHistory(__instance);
            foreach (QuestingBotsBotJobAssignmentHistoryEntry historyEntry in allHistoryEntries)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"Start={historyEntry.StartTimestamp}");
                sb.Append($", End={historyEntry.EndTimestamp}");
                sb.Append($", Quest={historyEntry.QuestName}");
                sb.Append($", Objective={historyEntry.QuestObjectiveName}");
                sb.Append($", Step={historyEntry.QuestStep}");
                sb.Append($", Status={historyEntry.Status}");
                LoggingController.LogInfo(sb.ToString());
            }

            LoggingController.LogInfo("----------------------------------");
        }
    }
}
