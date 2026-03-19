using QuestingBots.Configuration;
using QuestingBots.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using System.Reflection;

namespace QuestingBots.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class ConfigUtil
    {
        private const string FILENAME_CONFIG = "config.json";
        private const string FILENAME_EFT_QUEST_SETTINGS = "eftQuestSettings.json";
        private const string FILENAME_ZONE_AND_ITEM_QUEST_POSITIONS = "zoneAndItemQuestPositions.json";

        protected virtual string ConfigFileDirectory => ServerModDirectory;

        private string _serverModDirectory = null!;
        public string ServerModDirectory
        {
            get
            {
                if (_serverModDirectory == null)
                {
                    _serverModDirectory = GetServerModDirectory();
                }

                return _serverModDirectory;
            }
        }

        private ModConfig _currentConfig = null!;
        public ModConfig CurrentConfig
        {
            get
            {
                if (_currentConfig == null)
                {
                    _currentConfig = GetObject<ModConfig>(FILENAME_CONFIG);
                }

                return _currentConfig;
            }
        }

        private Dictionary<string, Dictionary<string, object>> _eftQuestSettings = null!;
        public Dictionary<string, Dictionary<string, object>> EFTQuestSettings
        {
            get
            {
                if (_eftQuestSettings == null)
                {
                    _eftQuestSettings = GetObject<Dictionary<string, Dictionary<string, object>>>(FILENAME_EFT_QUEST_SETTINGS);
                }

                return _eftQuestSettings;
            }
        }

        private Dictionary<string, ZoneAndItemPositionInfoConfig> _zoneAndItemQuestPositions = null!;
        public Dictionary<string, ZoneAndItemPositionInfoConfig> ZoneAndItemQuestPositions
        {
            get
            {
                if (_zoneAndItemQuestPositions == null)
                {
                    _zoneAndItemQuestPositions = GetObject<Dictionary<string, ZoneAndItemPositionInfoConfig>>(FILENAME_ZONE_AND_ITEM_QUEST_POSITIONS);
                }

                return _zoneAndItemQuestPositions;
            }
        }

        private ModHelper _modHelper;

        public ConfigUtil(ModHelper modHelper)
        {
            _modHelper = modHelper;
        }

        private string GetServerModDirectory()
        {
            return _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        }

        private T GetObject<T>(string filename)
        {
            string fileText = File.ReadAllText(Path.Combine(ConfigFileDirectory, filename));
            T? obj = ConfigHelpers.Deserialize<T>(fileText);
            if (obj == null)
            {
                throw new InvalidOperationException($"Could not deserialize {filename}");
            }

            return obj;
        }
    }
}
