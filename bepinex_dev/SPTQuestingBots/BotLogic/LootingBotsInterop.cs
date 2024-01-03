using BepInEx.Bootstrap;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LootingBots
{
    internal static class LootingBotsInterop
    {
        private static bool _LootingBotsLoadedChecked = false;
        private static bool _LootingBotsInteropInited = false;

        private static bool _IsLootingBotsLoaded;
        private static Type _LootingBotsExternalType;
        private static MethodInfo _ForceBotToLootNowMethod;

        /**
         * Return true if Looting Bots is loaded in the client
         */
        public static bool IsLootingBotsLoaded()
        {
            // Only check for SAIN once
            if (!_LootingBotsLoadedChecked)
            {
                _LootingBotsLoadedChecked = true;
                _IsLootingBotsLoaded = Chainloader.PluginInfos.ContainsKey("me.skwizzy.lootingbots");
            }

            return _IsLootingBotsLoaded;
        }

        /**
         * Initialize the Looting Bots interop class data, return true on success
         */
        public static bool Init()
        {
            if (!IsLootingBotsLoaded()) return false;

            // Only check for the External class once
            if (!_LootingBotsInteropInited)
            {
                _LootingBotsInteropInited = true;

                _LootingBotsExternalType = Type.GetType("LootingBots.External, skwizzy.LootingBots");

                // Only try to get the methods if we have the type
                if (_LootingBotsExternalType != null)
                {
                    _ForceBotToLootNowMethod = AccessTools.Method(_LootingBotsExternalType, "ForceBotToLootNow");
                }
            }

            // If we found the External class, at least some of the methods are (probably) available
            return (_LootingBotsExternalType != null);
        }

        /**
         * Force a bot to search for loot immediately if Looting Bots is loaded. Return true if successful.
         */
        public static bool TryForceBotToLootNow(BotOwner botOwner, float duration)
        {
            if (!Init()) return false;
            if (_ForceBotToLootNowMethod == null) return false;

            return (bool)_ForceBotToLootNowMethod.Invoke(null, new object[] { botOwner, duration });
        }
    }
}
