using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using EFT;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Extract;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Hearing;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Loot;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.BotLogic.ExternalMods.ModInfo
{
    public abstract class AbstractExternalModInfo
    {
        public abstract string GUID { get; }

        public virtual Version MinCompatibleVersion => new Version("0.0.0");
        public virtual Version MaxCompatibleVersion => new Version("9999.9999.9999");

        public bool IsInstalled { get; private set; } = false;

        public virtual string IncompatibilityMessage => "";
        public virtual bool IsCompatible() => IsVersionCompatible();

        public virtual bool CanUseInterop { get; protected set; } = false;
        public virtual bool CheckInteropAvailability() => false;

        public bool CheckIfInstalled()
        {
            IsInstalled = Chainloader.PluginInfos.Any(p => p.Value.Metadata.GUID == GUID);
            return IsInstalled;
        }

        public virtual AbstractExtractFunction CreateExtractFunction(BotOwner _botOwner) => new InternalExtractFunction(_botOwner);
        public virtual AbstractHearingFunction CreateHearingFunction(BotOwner _botOwner) => new InternalHearingFunction(_botOwner);
        public virtual AbstractLootFunction CreateLootFunction(BotOwner _botOwner) => new InternalLootFunction(_botOwner);

        public virtual Version GetVersion()
        {
            IEnumerable<BepInEx.PluginInfo> matchingPlugins = Chainloader.PluginInfos
                .Where(p => p.Value.Metadata.GUID == GUID)
                .Select(p => p.Value);

            if (!matchingPlugins.Any())
            {
                return null;
            }

            return matchingPlugins.First().Metadata.Version;
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
    }
}
