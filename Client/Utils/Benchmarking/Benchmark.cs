using Comfort.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuestingBots.Utils.Benchmarking
{
    public class BenchmarkResult
    {
        public double StartTime { get; private set; }
        public MethodBase Method { get; private set; }
        public double ElapsedMilliseconds { get; private set; }
        public long AllocatedMemory { get; private set; }

        public BenchmarkResult(double startTime, MethodBase method, double elapsedMilliseconds, long allocatedMemory)
        {
            StartTime = startTime;
            Method = method;
            ElapsedMilliseconds = elapsedMilliseconds;
            AllocatedMemory = allocatedMemory;
        }
    }

    public static class Benchmark
    {
        private static Stopwatch _overallStopwatch = Stopwatch.StartNew();
        private static Stopwatch _benchmarkStopwatch = new Stopwatch();
        private static long _benchmarkStartingMemory = long.MinValue;
        private static List<BenchmarkResult> _results = new List<BenchmarkResult> ();

        public static void Start(MethodBase methodToBenchmark)
        {
            if (!QuestingBotsPluginConfig.EnableBenchmarking.Value)
            {
                return;
            }

            Singleton<LoggingUtil>.Instance.LogDebug("Beginning benchmark for " + methodToBenchmark.Name + "...");
            
            if (_results.Count == 0)
            {
                _overallStopwatch.Restart();
            }

            _benchmarkStopwatch.Restart();
            _benchmarkStartingMemory = GetCurrentlyAllocatedMemory();
        }

        public static void Stop(MethodBase methodToBenchmark)
        {
            long benchmarkEndingMemory = GetCurrentlyAllocatedMemory();
            if (!_benchmarkStopwatch.IsRunning)
            {
                return;
            }
            _benchmarkStopwatch.Stop();

            if (!QuestingBotsPluginConfig.EnableBenchmarking.Value)
            {
                return;
            }

            double currentTime = _overallStopwatch.ElapsedMillisecondsAsDouble();
            double elapsedTime = _benchmarkStopwatch.ElapsedMillisecondsAsDouble();
            long memoryAllocated = benchmarkEndingMemory - _benchmarkStartingMemory;

            Singleton<LoggingUtil>.Instance.LogDebug("Beginning benchmark for " + methodToBenchmark.Name + "...Complete. Time = " + elapsedTime + " ms. Allocation = " + memoryAllocated + " bytes");

            BenchmarkResult result = new BenchmarkResult(currentTime, methodToBenchmark, elapsedTime, memoryAllocated);
            _results.Add(result);
        }

        private static long GetCurrentlyAllocatedMemory() => GC.GetTotalMemory(false);

        public static void LogAllBenchmarksAndReset()
        {
            _overallStopwatch.Stop();

            if (_results.Count > 0)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Saving benchmarking log file...");
                Singleton<LoggingUtil>.Instance.LogDebug("Time (ms), Method Name, ET (ms), Allocation (bytes)");

                foreach (BenchmarkResult result in _results)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug(result.StartTime + "," + result.Method.Name + "," + result.ElapsedMilliseconds + "," + result.AllocatedMemory);
                }

                Singleton<LoggingUtil>.Instance.LogDebug("Saving benchmarking log file...done.");
            }

            _results.Clear();
        }
    }
}
