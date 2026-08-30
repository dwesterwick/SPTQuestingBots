using QuestingBots.Server.Internal;
using QuestingBots.Utils;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Server;
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

        [SetUp]
        public void Setup()
        {
            RunFromSptInstallDirectoryService.RunFromSptInstallDirectory(LoadSptDependencies);

            _logger = new MockLogger<QuestingBots_Server>();
            _configUtil = new MockConfigUtil(_modHelper);
            _loggingUtil = new LoggingUtil(_logger, _configUtil);
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }

        private void LoadSptDependencies()
        {
            _modHelper = DI.GetInstance().GetService<ModHelper>();
        }
    }
}
