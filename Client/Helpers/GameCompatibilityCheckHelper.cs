using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using QuestingBots.Utils;

namespace QuestingBots.Helpers
{
    public static class GameCompatibilityCheckHelper
    {
        private static readonly string sptCommonAssemblyName = "spt-common";

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
                Singleton<LoggingUtil>.Instance.LogError("An exception occurred when getting the SPT version: " + e.Message);
                return null!;
            }
        }

        public static bool IsSPTWithinVersionRange(string minVersionString, string maxVersionString, out string currentVersionString)
        {
            if (!Version.TryParse(minVersionString, out Version minVersion))
            {
                throw new FormatException($"Cannot parse version {minVersion}");
            }

            if (!Version.TryParse(maxVersionString, out Version maxVersion))
            {
                throw new FormatException($"Cannot parse version {maxVersion}");
            }

            return IsSPTWithinVersionRange(minVersion, maxVersion, out currentVersionString);
        }

        public static bool IsSPTWithinVersionRange(Version minVersion, Version maxVersion, out string currentVersionString)
        {
            currentVersionString = GetSPTVersionString();
            if (currentVersionString == null)
            {
                Singleton<LoggingUtil>.Instance.LogErrorToServerConsole("Could not determine the current SPT version.");
                return false;
            }

            if (!Version.TryParse(currentVersionString, out Version currentVersion))
            {
                throw new FormatException($"Cannot parse SPT version {currentVersionString}");
            }

            return currentVersion.IsCompatible(minVersion, maxVersion);
        }

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

        public static bool IsNvidiaReflexEnabled()
        {
            if (!Singleton<SharedGameSettingsClass>.Instantiated)
            {
                return false;
            }

            EFT.Settings.Graphics.ENvidiaReflexMode reflexMode = Singleton<SharedGameSettingsClass>.Instance.Graphics.Settings.NVidiaReflex;

            return reflexMode != EFT.Settings.Graphics.ENvidiaReflexMode.Off;
        }
    }
}
