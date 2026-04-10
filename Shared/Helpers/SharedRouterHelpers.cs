using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class SharedRouterHelpers
    {
        public static string GetRoutePath(string routeName)
        {
            if (routeName.Contains("/"))
            {
                return routeName;
            }

            return $"/{ModInfo.MODNAME}/{routeName}";
        }
    }
}
