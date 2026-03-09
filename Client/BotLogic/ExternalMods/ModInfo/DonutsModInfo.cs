using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestingBots.Controllers;

namespace QuestingBots.BotLogic.ExternalMods.ModInfo
{
    public class DonutsModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "com.dvize.Donuts";

        public override string IncompatibilityMessage => "Using Questing Bots spawns with DONUTS may result in too many spawns. Use at your own risk.";
        public override bool IsCompatible() => !ConfigController.Config.BotSpawns.Enabled;

        public DonutsModInfo()
        {

        }
    }
}
