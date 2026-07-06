using Comfort.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuestingBots.Utils.Benchmarking
{
    public enum BenchmarkStatus
    {
        NotStarted,
        Running,
        Complete,
        Aborted,
    }

    public class Benchmark
    {
        public BenchmarkStatus Status { get; private set; } = BenchmarkStatus.NotStarted;
        public MethodBase Method { get; private set; }
        public long StartingMemory { get; private set; } = long.MinValue;
        public long EndingMemory { get; private set; } = long.MinValue;

        private Stopwatch _benchmarkStopwatch = new Stopwatch();

        public bool IsRunning => Status == BenchmarkStatus.Running;
        
        public Benchmark(MethodBase method)
        {
            Method = method;
        }

        public void Start()
        {
            if (BenchmarkService.ShowDebugMessages)
            {
                Singleton<LoggingUtil>.Instance.LogDebug($"Running benchmark for {Method.Name}...");
            }

            Status = BenchmarkStatus.Running;

            _benchmarkStopwatch.Restart();
            StartingMemory = BenchmarkService.GetCurrentlyAllocatedMemory();
        }

        public void Stop()
        {
            Stop_Internal();
            Status = BenchmarkStatus.Complete;

            if (BenchmarkService.ShowDebugMessages)
            {
                Singleton<LoggingUtil>.Instance.LogDebug($"Running benchmark for {Method.Name}...done.");
            }
        }

        private void Stop_Internal()
        {
            EndingMemory = BenchmarkService.GetCurrentlyAllocatedMemory();
            _benchmarkStopwatch.Stop();
        }

        public void Abort()
        {
            Stop_Internal();
            Status = BenchmarkStatus.Aborted;

            if (BenchmarkService.ShowDebugMessages)
            {
                Singleton<LoggingUtil>.Instance.LogWarning($"Running benchmark for {Method.Name}...aborted!");
            }
        }

        public void AbortIfRunning()
        {
            if (IsRunning)
            {
                Abort();
            }
        }

        public BenchmarkResult GetResult(double overallElapsedTime)
        {
            BenchmarkResult result = new BenchmarkResult(overallElapsedTime, Method, _benchmarkStopwatch, StartingMemory, EndingMemory);

            if (BenchmarkService.ShowDebugMessages)
            {
                Singleton<LoggingUtil>.Instance.LogDebug($"Result for {Method.Name}: Time = {result.GetElapsedMilliseconds()} ms. Allocation = {result.MemoryAllocated} bytes");
            }

            return result;
        }
    }
}
