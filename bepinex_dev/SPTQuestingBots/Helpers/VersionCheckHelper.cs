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
        public static bool IsSPTWithinVersionRange(string minVersionString, string maxVersionString, out string currentVersionString)
        {
            currentVersionString = "???";

            try
            {
                string assemblyName = "Aki.Common";
                Assembly assembly = Assembly.Load(assemblyName);
                if (assembly == null)
                {
                    LoggingController.LogError("Could not find assembly " + assemblyName);
                    return false;
                }

                currentVersionString = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
                Version actualVersion = new Version(currentVersionString);
                Version minVersion = new Version(minVersionString);
                Version maxVersion = new Version(maxVersionString);

                if (actualVersion.CompareTo(minVersion) < 0)
                {
                    return false;
                }
                if (actualVersion.CompareTo(maxVersion) > 0)
                {
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
