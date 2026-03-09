using QuestingBots.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class SharedConfigHelpers
    {
        public static bool IsModEnabled(this ModConfig? modConfig) => modConfig?.Enabled == true;
        public static bool IsDebugEnabled(this ModConfig? modConfig) => modConfig?.Debug == true;

        public static void DisableMod(this ModConfig modConfig) => modConfig.Enabled = false;
    }
}
