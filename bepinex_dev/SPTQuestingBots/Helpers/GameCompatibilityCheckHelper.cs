using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Helpers
{
    public static class GameCompatibilityCheckHelper
    {
        private static string sptCommonAssemblyName = "spt-common";

        public static bool IsCompatible(this Version actualVersion, Version minVersion, Version maxVersion)
        {
            if (actualVersion.CompareTo(minVersion) < 0)
            {
                return false;
            }
            if (actualVersion.CompareTo(maxVersion) > 0)
            {
                return false;
            }

            return true;
        }

        public static bool IsSPTWithinVersionRange(string minVersionString, string maxVersionString, out string currentVersionString)
        {
            currentVersionString = GetSPTVersionString();
            if (currentVersionString == null)
            {
                LoggingController.LogErrorToServerConsole("Could not determine the current SPT version.");
                return false;
            }

            Version currentVersion = new Version(currentVersionString);
            Version minVersion = new Version(minVersionString);
            Version maxVersion = new Version(maxVersionString);

            return currentVersion.IsCompatible(minVersion, maxVersion);
        }

        public static bool IsNvidiaReflexEnabled()
        {
            if (!Singleton<SharedGameSettingsClass>.Instantiated)
            {
                return false;
            }

            EFT.Settings.Graphics.ENvidiaReflexMode reflexMode = Singleton<SharedGameSettingsClass>.Instance.Graphics.Settings.NVidiaReflex;

            return reflexMode != EFT.Settings.Graphics.ENvidiaReflexMode.Off;
        }

        public static string GetSPTVersionString()
        {
            try
            {
                Assembly assembly = Assembly.Load(sptCommonAssemblyName);
                if (assembly == null)
                {
                    throw new InvalidOperationException("Could not find assembly " + sptCommonAssemblyName);
                }

                return System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
            }
            catch (Exception e)
            {
                LoggingController.LogError("An exception occurred when getting the SPT version: " + e.Message);
                return null;
            }
        }
    }
}
