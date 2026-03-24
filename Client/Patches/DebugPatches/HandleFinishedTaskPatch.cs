using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using QuestingBots.Utils;
using SPT.Reflection.Patching;

namespace QuestingBots.Patches.DebugPatches
{
    public class HandleFinishedTaskPatch : AbstractHandleFinishedTaskPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TasksExtensions).GetMethod(
                nameof(TasksExtensions.HandleFinishedTask),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(Task) },
                null);
        }

        [PatchPrefix]
        protected static void PatchPrefix(Task task) => LogException(task);
    }

    public class HandleFinishedTaskPatch2 : AbstractHandleFinishedTaskPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TasksExtensions).GetMethod(
                nameof(TasksExtensions.HandleFinishedTask),
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new Type[] { typeof(Task), typeof(object) },
                null);
        }

        [PatchPrefix]
        protected static void PatchPrefix(Task task, object errorMessage) => LogException(task);
    }

    public abstract class AbstractHandleFinishedTaskPatch : ModulePatch
    {
        protected static void LogException(Task task)
        {
            if (task.IsFaulted)
            {
                Singleton<LoggingUtil>.Instance.LogError("Error found in HandleFinishedTask: " + task.Exception.Message);
                Singleton<LoggingUtil>.Instance.LogError(task.Exception.StackTrace);
            }
        }
    }
}
