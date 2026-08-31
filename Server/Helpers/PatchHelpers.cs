using SPTarkov.Reflection.Patching;

namespace QuestingBots.Helpers
{
    public static class PatchHelpers
    {
        public static IRuntimePatch? FindPatch<T>(this IEnumerable<IRuntimePatch> patches)
        {
            foreach (IRuntimePatch patch in patches)
            {
                if (patch.GetType() == typeof(T))
                {
                    return patch;
                }
            }

            return null;
        }

        public static bool TryEnablePatch<T>(this IEnumerable<IRuntimePatch> patches)
        {
            IRuntimePatch? patch = FindPatch<T>(patches);
            if (patch == null)
            {
                return false;
            }

            patch.Enable();
            return true;
        }

        public static bool TryDisablePatch<T>(this IEnumerable<IRuntimePatch> patches)
        {
            IRuntimePatch? patch = FindPatch<T>(patches);
            if (patch == null)
            {
                return false;
            }

            patch.Disable();
            return true;
        }
    }
}
