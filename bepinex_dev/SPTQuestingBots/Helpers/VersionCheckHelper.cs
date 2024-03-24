using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Helpers
{
    public class VersionCheckHelper
    {
        public static bool IsSPTWithinVersionRange(string minVersionString, string maxVersionString)
        {
            try
            {
                Assembly assembly = Assembly.Load("Aki.Common");
                if (assembly == null)
                {
                    LoggingController.LogError("Could not find assembly Aki.Common");
                    return false;
                }

                Version actualVersion = new Version(System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion);
                Version minVersion = new Version(minVersionString);
                Version maxVersion = new Version(maxVersionString);

                if (actualVersion.CompareTo(minVersion) < 0)
                {
                    LoggingController.LogError("SPT-AKI " + minVersionString + " or later is required. Current version is " + actualVersion.ToString());
                    return false;
                }
                if (actualVersion.CompareTo(maxVersion) > 0)
                {
                    LoggingController.LogError("SPT-AKI " + maxVersionString + " or below is required. Current version is " + actualVersion.ToString());
                    return false;
                }
            }
            catch (Exception e)
            {
                LoggingController.LogError("An exception occurred when checking the current SPT-AKI version: " + e.Message);
                return false;
            }

            return true;
        }
    }
}
