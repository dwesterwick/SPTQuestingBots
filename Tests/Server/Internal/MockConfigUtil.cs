using QuestingBots.Utils;
using SPTarkov.Server.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Server.Internal
{
    internal class MockConfigUtil : ConfigUtil
    {
        private string _configFileDirectory = null!;
        protected override string ConfigFileDirectory
        {
            get
            {
                if (_configFileDirectory == null)
                {
                    _configFileDirectory = GetConfigFileDirectory();
                }

                return _configFileDirectory;
            }
        }

        public MockConfigUtil(ModHelper modHelper) : base(modHelper) { }

        private string GetConfigFileDirectory()
        {
            string cd = Directory.GetCurrentDirectory();

            string configDirectory = Path.Combine(cd, "config");
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            string configFile = Path.Combine(configDirectory, "config.json");
            if (!File.Exists(configFile))
            {
                string sourceConfigFile = Path.GetFullPath(Path.Combine(cd, "..\\..\\..\\..\\Shared\\Config\\config.json"));
                if (!File.Exists(sourceConfigFile))
                {
                    throw new FileNotFoundException($"Cannot find {sourceConfigFile}");
                }

                File.Copy(sourceConfigFile, configFile);
            }

            return configDirectory;
        }
    }
}
