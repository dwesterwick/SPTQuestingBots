using QuestingBots.Server.Internal;
using QuestingBots.Services;
using QuestingBots.Utils;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Server
{
    public class DatabaseModificationTests
    {
        private ISptLogger<QuestingBots_Server> _logger;
        private LoggingUtil _loggingUtil;
        private MockConfigUtil _configUtil;

        private ModHelper _modHelper = null!;
        private PmcConfig _pmcConfig = null!;
        private BotConfig _botConfig = null!;

        private UpdatePMCAndPScavBrainTypesService _updatePMCAndPScavBrainTypesService;

        [SetUp]
        public void Setup()
        {
            RunFromSptInstallDirectoryService.RunFromSptInstallDirectory(LoadSptDependencies);

            _logger = new MockLogger<QuestingBots_Server>();
            _configUtil = new MockConfigUtil(_modHelper);
            _loggingUtil = new LoggingUtil(_logger, _configUtil);

            _updatePMCAndPScavBrainTypesService = new UpdatePMCAndPScavBrainTypesService(_loggingUtil, _configUtil, _pmcConfig, _botConfig);
        }

        [Test]
        public void EnsurePMCBrainsCanBeBlacklisted()
        {
            IEnumerable<string> blacklistedbrainTypes = _configUtil.CurrentConfig.BotSpawns.BlacklistedPMCBotBrains;
            Assert.Greater(blacklistedbrainTypes.Count(), 0);

            int baselineCount = CountBlacklistedPMCBrains(blacklistedbrainTypes);
            if (baselineCount == 0)
            {
                return;
            }

            _updatePMCAndPScavBrainTypesService.RemoveBlacklistedPMCBrains(blacklistedbrainTypes);

            int updatedCount = CountBlacklistedPMCBrains(blacklistedbrainTypes);
            Assert.AreEqual(updatedCount, 0);
        }

        private int CountBlacklistedPMCBrains(IEnumerable<string> blacklistedbrainTypes)
        {
            int matches = 0;
            foreach (string pmcType in _pmcConfig.PmcType.Keys)
            {
                foreach (string map in _pmcConfig.PmcType[pmcType].Keys)
                {
                    foreach (string blacklistedBrain in blacklistedbrainTypes)
                    {
                        matches += _pmcConfig.PmcType[pmcType][map].ContainsKey(blacklistedBrain) ? 1 : 0;
                    }
                }
            }

            return matches;
        }

        [Test]
        public void EnsurePlayerScavBrainsCanBeBlacklisted()
        {
            IEnumerable<string> blacklistedbrainTypes = _configUtil.CurrentConfig.BotSpawns.BlacklistedPMCBotBrains;
            Assert.Greater(blacklistedbrainTypes.Count(), 0);

            int baselineCount = CountBlacklistedPlayerScavBrains(blacklistedbrainTypes);
            if (baselineCount == 0)
            {
                return;
            }

            _updatePMCAndPScavBrainTypesService.RemoveBlacklistedPlayerScavBrains(blacklistedbrainTypes);

            int updatedCount = CountBlacklistedPlayerScavBrains(blacklistedbrainTypes);
            Assert.AreEqual(updatedCount, 0);
        }

        private int CountBlacklistedPlayerScavBrains(IEnumerable<string> blacklistedbrainTypes)
        {
            int matches = 0;
            foreach (string map in _botConfig.PlayerScavBrainType.Keys)
            {
                foreach (string blacklistedBrain in blacklistedbrainTypes)
                {
                    matches += _botConfig.PlayerScavBrainType[map].ContainsKey(blacklistedBrain) ? 1 : 0;
                }
            }

            return matches;
        }

        private void LoadSptDependencies()
        {
            _modHelper = DI.GetInstance().GetService<ModHelper>();
            _pmcConfig = DI.GetInstance().GetService<PmcConfig>();
            _botConfig = DI.GetInstance().GetService<BotConfig>();
        }
    }
}
