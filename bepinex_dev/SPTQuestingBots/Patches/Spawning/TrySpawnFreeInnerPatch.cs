using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches.Spawning
{
    public class TrySpawnFreeInnerPatch : TrySpawnFreeInnerAbstractPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1627).GetMethod(nameof(GClass1627.TrySpawnFreeInner), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static void PatchPrefix(ref List<Player> ____allPlayers) => ReplaceAllPlayersList(ref ____allPlayers);

        [PatchPostfix]
        protected static void PatchPostfix(ref List<Player> ____allPlayers) => RestoreAllPlayersList(ref ____allPlayers);
    }

    public class TrySpawnFreeInnerPatch2 : TrySpawnFreeInnerAbstractPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1628).GetMethod(nameof(GClass1628.TrySpawnFreeInner), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static void PatchPrefix(ref List<Player> ____allPlayers) => ReplaceAllPlayersList(ref ____allPlayers);

        [PatchPostfix]
        protected static void PatchPostfix(ref List<Player> ____allPlayers) => RestoreAllPlayersList(ref ____allPlayers);
    }

    public abstract class TrySpawnFreeInnerAbstractPatch : ModulePatch
    {
        private static Player[] cachedAllPlayers = null;

        protected static void ReplaceAllPlayersList(ref List<Player> ____allPlayers)
        {
            cachedAllPlayers = ____allPlayers.ToArray();

            ____allPlayers = Helpers.BotBrainHelpers.GetAllHumanAndSimulatedPlayers().ToList();
        }

        protected static void RestoreAllPlayersList(ref List<Player> ____allPlayers)
        {
            ____allPlayers = cachedAllPlayers.ToList();

            cachedAllPlayers = null;
        }
    }
}
