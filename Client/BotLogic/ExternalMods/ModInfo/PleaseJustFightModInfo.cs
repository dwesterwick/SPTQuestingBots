using Comfort.Common;
using QuestingBots.Controllers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.ExternalMods.ModInfo
{
    public class PleaseJustFightModInfo : AbstractExternalModInfo
    {
        public override string GUID => "Shibdib.PleaseJustFight";

        public override string IncompatibilityMessage => "\"Please Just Fight\" is not compatible with the QB spawning system";
        public override bool IsCompatible() => !Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled || base.IsCompatible();

        public PleaseJustFightModInfo()
        {

        }
    }
}
