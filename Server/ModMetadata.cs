using SPTarkov.Server.Core.Models.Spt.Mod;

namespace QuestingBots
{
    public record ModMetadata : AbstractModMetadata
    {
        public override string ModGuid { get; init; } = ModInfo.GUID;
        public override string Name { get; init; } = ModInfo.MODNAME;
        public override string Author { get; init; } = ModInfo.AUTHOR;
        public override List<string>? Contributors { get; init; }
        public override SemanticVersioning.Version Version { get; init; } = new(ModInfo.MOD_VERSION);
        public override SemanticVersioning.Range SptVersion { get; init; } = new(ModInfo.SPT_VERSION_COMPATIBILITY);
        public override List<string>? Incompatibilities { get; init; }
        public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public override string? Url { get; init; }
        public override bool? IsBundleMod { get; init; } = false;
        public override string License { get; init; } = "CC BY-NC-SA 4.0";

        public string RelativePathToSptInstall = ModInfo.RELATIVE_PATH_TO_SPT_INSTALL;
    }
}
