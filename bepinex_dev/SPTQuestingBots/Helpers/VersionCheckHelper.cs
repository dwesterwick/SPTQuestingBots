using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Helpers
{
    public class VersionCheckHelper
    {
        private static string sptCommonAssemblyName = "spt-common";
        private static string performanceImprovementsGuid = "com.dirtbikercj.performanceImprovements";
        private static Version lowestIncompatiblePerformanceImprovements = new Version("0.2.1");
        private static Version highestIncompatiblePerformanceImprovements = new Version("0.2.3");

        public static bool IsSPTWithinVersionRange(string minVersionString, string maxVersionString, out string currentVersionString)
        {
            currentVersionString = "???";

            try
            {
                Assembly assembly = Assembly.Load(sptCommonAssemblyName);
                if (assembly == null)
                {
                    LoggingController.LogError("Could not find assembly " + sptCommonAssemblyName);
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
                LoggingController.LogError("An exception occurred when checking the current SPT version: " + e.Message);
                return false;
            }

            return true;
        }

        public static bool IsPerformanceImprovementsVersionCompatible()
        {
            IEnumerable<BepInEx.PluginInfo> matchingPerformanceImprovementsPlugins = Chainloader.PluginInfos
                .Where(p => p.Value.Metadata.GUID == performanceImprovementsGuid)
                .Select(p => p.Value);

            if (!matchingPerformanceImprovementsPlugins.Any())
            {
                return true;
            }

            Version actualVersion = matchingPerformanceImprovementsPlugins.First().Metadata.Version;

            // Versions 0.2.1 through 0.2.3 caused bot freezing problems
            if (actualVersion.CompareTo(lowestIncompatiblePerformanceImprovements) < 0)
            {
                return true;
            }
            if (actualVersion.CompareTo(highestIncompatiblePerformanceImprovements) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
