using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class SharedMathHelpers
    {
        public static bool IsAnInteger(this float value) => value % 1.0 == 0;
        public static bool IsAnInteger(this double value) => value % 1.0 == 0;
    }
}
