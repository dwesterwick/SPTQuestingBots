using SPTarkov.Server.Core.Models.Spt.Mod;

namespace QuestingBots
{
    public record ModMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = ModInfo.GUID;
        public string Name { get; init; } = ModInfo.MODNAME;
        public string Author { get; init; } = ModInfo.AUTHOR;
        public List<string>? Contributors { get; init; }
        public SemanticVersioning.Version Version { get; init; } = new(ModInfo.MOD_VERSION);
        public SemanticVersioning.Range SptVersion { get; init; } = new(ModInfo.SPT_VERSION_COMPATIBILITY);
        public List<string>? Incompatibilities { get; init; }
        public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public string? Url { get; init; }
        public bool? IsBundleMod { get; init; } = false;
        public string License { get; init; } = "CC BY-NC-SA 4.0";
        public bool HasPrepatcher { get; init; } = false;

        public string RelativePathToSptInstall = ModInfo.RELATIVE_PATH_TO_SPT_INSTALL;
    }
}
