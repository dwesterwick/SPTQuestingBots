using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.ModInfo
{
    public class PerformanceImprovementsModInfo : AbstractExternalModInfo
    {
        public override string GUID => "com.dirtbikercj.performanceImprovements";

        public override Version MinCompatibleVersion => new Version("0.2.3");
        public override Version MaxCompatibleVersion => new Version("0.2.1");

        public override string IncompatibilityMessage => "Performance Improvements versions 0.2.1 - 0.2.3 are not compatible with Questing Bots. Please upgrade Performance Improvements to 0.2.4 or newer to use with Questing Bots.";

        public PerformanceImprovementsModInfo()
        {

        }
    }
}
