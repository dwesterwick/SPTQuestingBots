using BepInEx;
using BepInEx.Bootstrap;
using EFT;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Extract;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Hearing;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Loot;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.ModInfo
{
    public abstract class AbstractExternalModInfo
    {
        public abstract string GUID { get; }

        public virtual Version MinCompatibleVersion => new Version("0.0.0");
        public virtual Version MaxCompatibleVersion => new Version("9999.9999.9999");

        public bool IsInstalled { get; private set; } = false;
        public PluginInfo PluginInfo { get; private set; } = null;

        public virtual string IncompatibilityMessage => "";
        public virtual bool IsCompatible() => IsVersionCompatible();

        public virtual bool CanUseInterop { get; protected set; } = false;
        public virtual bool CheckInteropAvailability() => false;

        private bool checkedIfInstalled = false;

        public virtual AbstractExtractFunction CreateExtractFunction(BotOwner _botOwner) => new InternalExtractFunction(_botOwner);
        public virtual AbstractHearingFunction CreateHearingFunction(BotOwner _botOwner) => new InternalHearingFunction(_botOwner);
        public virtual AbstractLootFunction CreateLootFunction(BotOwner _botOwner) => new InternalLootFunction(_botOwner);

        public bool CheckIfInstalled()
        {
            checkedIfInstalled = true;

            IEnumerable<PluginInfo> matchingPlugins = Chainloader.PluginInfos
                .Where(p => p.Value.Metadata.GUID == GUID)
                .Select(p => p.Value);

            if (!matchingPlugins.Any())
            {
                return false;
            }

            if (matchingPlugins.Count() > 1)
            {
                LoggingController.LogError("Found multiple instances of plugins with GUID " + GUID + ". Interoperability disabled.");
                return false;
            }

            PluginInfo = matchingPlugins.First();
            IsInstalled = true;

            return IsInstalled;
        }

        public bool IsVersionCompatible()
        {
            Version actualVersion = GetVersion();
            if (actualVersion == null)
            {
                return true;
            }

            return actualVersion.IsCompatible(MinCompatibleVersion, MaxCompatibleVersion);
        }

        public Version GetVersion()
        {
            if (!checkedIfInstalled)
            {
                CheckIfInstalled();
            }

            return PluginInfo?.Metadata?.Version;
        }

        public string GetName()
        {
            if (!checkedIfInstalled)
            {
                CheckIfInstalled();
            }

            return PluginInfo?.Metadata?.Name;
        }
    }
}
