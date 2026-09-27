using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class SharedMathHelpers
    {
        public static bool IsAnInteger(this float value) => value % 1.0 == 0;
        public static bool IsAnInteger(this double value) => value % 1.0 == 0;

        public static int ClampPercentage(this int percentage) => Math.Clamp(percentage, 0, 100);
    }
}
