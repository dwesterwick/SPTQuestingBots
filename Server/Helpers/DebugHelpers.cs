using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Helpers
{
    public static class DebugHelpers
    {
        public static bool IsReleaseBuild() => ProgramStatics.ENTRY_TYPE() == SPTarkov.Server.Core.Models.Enums.EntryType.RELEASE;

        public static bool IsDebugConfiguration()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
