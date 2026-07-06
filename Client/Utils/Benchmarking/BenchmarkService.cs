using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine.Profiling;

namespace QuestingBots.Utils.Benchmarking
{
    internal static class BenchmarkService
    {
        public static bool ShowDebugMessages { get; set; } = false;

        private static Stopwatch _overallStopwatch = Stopwatch.StartNew();
        private static Dictionary<MethodBase, Benchmark> _activeBenchmarks = new Dictionary<MethodBase, Benchmark>();
        private static List<BenchmarkResult> _results = new List<BenchmarkResult>();

        public static double GetOverallElapsedTime() => _overallStopwatch.ElapsedMillisecondsAsDouble();
        public static long GetCurrentlyAllocatedMemory() => Profiler.GetTotalAllocatedMemoryLong();

        public static bool IsBenchmarkRunning(MethodBase method)
        {
            return _activeBenchmarks.ContainsKey(method) && _activeBenchmarks[method].IsRunning;
        }

        public static void Start(MethodBase methodToBenchmark)
        {
            if (!QuestingBotsPluginConfig.EnableBenchmarking.Value)
            {
                return;
            }

            if (_results.Count == 0)
            {
                _overallStopwatch.Restart();
            }

            AddNewBenchmark(methodToBenchmark);
            _activeBenchmarks[methodToBenchmark].Start();
        }

        public static void Stop(MethodBase methodToBenchmark)
        {
            if (!QuestingBotsPluginConfig.EnableBenchmarking.Value)
            {
                return;
            }

            if (!IsBenchmarkRunning(methodToBenchmark))
            {
                return;
            }

            _activeBenchmarks[methodToBenchmark].Stop();

            double overallElapsedTime = GetOverallElapsedTime();
            BenchmarkResult result = _activeBenchmarks[methodToBenchmark].GetResult(overallElapsedTime);
            _results.Add(result);
        }

        private static void AddNewBenchmark(MethodBase methodToBenchmark)
        {
            Benchmark benchmark = new Benchmark(methodToBenchmark);

            if (_activeBenchmarks.ContainsKey(methodToBenchmark))
            {
                _activeBenchmarks[methodToBenchmark].AbortIfRunning();
                _activeBenchmarks[methodToBenchmark] = benchmark;
            }
            else
            {
                _activeBenchmarks.Add(methodToBenchmark, benchmark);
            }
        }

        private static void AbortAllRunningBenchmarks()
        {
            foreach (Benchmark benchmark in _activeBenchmarks.Values)
            {
                benchmark.AbortIfRunning();
            }
        }

        public static void LogAllBenchmarksAndReset(long timestamp)
        {
            _overallStopwatch.Stop();

            AbortAllRunningBenchmarks();

            if (QuestingBotsPluginConfig.EnableBenchmarking.Value && (_results.Count > 0))
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Writing benchmarking log file...");

                string logText = CreateBenchmarkingLogText();
                string locationId = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id;

                string filename = Singleton<LoggingUtil>.Instance.LoggingPath
                    + locationId.Replace(" ", "")
                    + "_"
                    + timestamp
                    + "_benchmarking.csv";

                Singleton<LoggingUtil>.Instance.CreateLogFile("benchmarking", filename, logText);
            }

            _results.Clear();
        }

        private static string CreateBenchmarkingLogText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Overall Time when Completed (ms),Time after Last Benchmark Completed (ms),Method Name,ET (ms),Allocation (bytes)");

            for (int i = 0; i < _results.Count; i++)
            {
                sb.Append(_results[i].OverallTime);
                sb.Append(", " + (i > 0 ? _results[i].OverallTime - _results[i - 1].OverallTime : 0));
                sb.Append("," + _results[i].GetMethodName());
                sb.Append("," + _results[i].GetElapsedMilliseconds());
                sb.AppendLine("," + _results[i].MemoryAllocated);
            }

            return sb.ToString();
        }
    }
}
