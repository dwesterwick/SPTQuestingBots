using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Helpers
{
    public class VersionCheckHelper
    {
        public static bool CheckSPTVersion()
        {
            try
            {
                string typeName = "Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil";
                Type raidTimeUtilType = AccessTools.TypeByName(typeName);
                if (raidTimeUtilType == null)
                {
                    throw new TypeAccessException("Cannot find type " + typeName);
                }

                string methodName = "HasRaidStarted";
                if (!AccessTools.GetMethodNames(raidTimeUtilType).Any(m => m == methodName))
                {
                    throw new MissingMethodException(typeName, methodName);
                }
            }
            catch (Exception ex)
            {
                if ((ex is TypeAccessException) || (ex is MissingMethodException))
                {
                    LoggingController.LogErrorToServerConsole("Cannot find methods for retrieving raid-time data. Please ensure you are using SPT-AKI 3.7.4 with the 2023-12-06 hotfix or newer.");

                    return false;
                }
            }

            return true;
        }
    }
}
