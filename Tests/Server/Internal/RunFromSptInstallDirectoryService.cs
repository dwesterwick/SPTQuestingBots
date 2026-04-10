using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Server.Internal
{
    internal static class RunFromSptInstallDirectoryService
    {
        // TO DO: Can this be read from build properties?
        private const string RELATIVE_PATH_TO_SPT_INSTALL_DIRECTORY = "..\\..\\";

        private static string _cd = null!;
        private static string CurrentDirectory
        {
            get
            {
                if (_cd == null)
                {
                    _cd = Directory.GetCurrentDirectory();
                }

                return _cd;
            }
        }

        private static string _pathToSptInstallDirectory = null!;
        private static string PathToSptInstallDirectory
        {
            get
            {
                if (_pathToSptInstallDirectory == null)
                {
                    _pathToSptInstallDirectory = GetPathToSptInstallDirectory();
                }

                return _pathToSptInstallDirectory;
            }
        }

        public static void RunFromSptInstallDirectory(Action action)
        {
            Directory.SetCurrentDirectory(PathToSptInstallDirectory);
            action();
            Directory.SetCurrentDirectory(CurrentDirectory);
        }

        public static TOut RunFromSptInstallDirectory<TIn, TOut>(Func<TIn, TOut> func, TIn value)
        {
            Directory.SetCurrentDirectory(PathToSptInstallDirectory);
            TOut output = func(value);
            Directory.SetCurrentDirectory(CurrentDirectory);

            return output;
        }

        private static string GetPathToSptInstallDirectory()
        {
            string sptInstallPath = Path.GetFullPath(Path.Combine(CurrentDirectory, RELATIVE_PATH_TO_SPT_INSTALL_DIRECTORY, "..\\..\\..\\..\\SPT"));
            if (!Directory.Exists(sptInstallPath))
            {
                throw new DirectoryNotFoundException($"Could not find directory {sptInstallPath}");
            }

            return sptInstallPath;
        }
    }
}
