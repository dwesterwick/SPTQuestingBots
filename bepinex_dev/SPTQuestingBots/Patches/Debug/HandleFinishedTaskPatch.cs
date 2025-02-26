using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches.Debug
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
                Controllers.LoggingController.LogError("Error found in HandleFinishedTask: " + task.Exception.Message);
                Controllers.LoggingController.LogError(task.Exception.StackTrace);
            }
        }
    }
}
