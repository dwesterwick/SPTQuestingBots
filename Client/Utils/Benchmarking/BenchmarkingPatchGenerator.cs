using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuestingBots.Utils.Benchmarking
{
    [HarmonyLib.HarmonyPatch]
    internal class BenchmarkingPatchGenerator
    {
        protected static IEnumerable<MethodBase> TargetMethods()
        {
            Type[] allTypes = typeof(QuestingBotsPlugin).Assembly.GetTypes();
            foreach (Type type in allTypes)
            {
                IEnumerable<MethodInfo> allMatchingMethods = HarmonyLib.AccessTools.GetDeclaredMethods(type)
                    .Where(m => m.GetCustomAttribute<BenchmarkAttribute>() != null);

                foreach (MethodInfo method in allMatchingMethods)
                {
                    yield return method;
                }
            }
        }

        [HarmonyLib.HarmonyPrefix]
        protected static void Prefix(MethodBase __originalMethod) => Benchmark.Start(__originalMethod);

        [HarmonyLib.HarmonyPostfix]
        protected static void Postfix(MethodBase __originalMethod) => Benchmark.Stop(__originalMethod);
    }
}
