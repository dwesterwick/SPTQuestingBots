using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using QuestingBots.Controllers;
using QuestingBots.Utils;

namespace QuestingBots.BotLogic.ExternalMods.ModInfo
{
    public class DonutsModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "com.dvize.Donuts";

        public override string IncompatibilityMessage => "Using Questing Bots spawns with DONUTS may result in too many spawns. Use at your own risk.";
        public override bool IsCompatible() => !Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled;

        public DonutsModInfo()
        {

        }
    }
}
