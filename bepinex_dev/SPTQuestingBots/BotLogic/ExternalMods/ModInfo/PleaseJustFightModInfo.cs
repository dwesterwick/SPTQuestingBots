using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.ModInfo
{
    public class PleaseJustFightModInfo : AbstractExternalModInfo
    {
        public override string GUID => "Shibdib.PleaseJustFight";

        public override string IncompatibilityMessage => "\"Please Just Fight\" is not compatible with the QB spawning system";
        public override bool IsCompatible() => !ConfigController.Config.BotSpawns.Enabled || base.IsCompatible();

        public PleaseJustFightModInfo()
        {

        }
    }
}
