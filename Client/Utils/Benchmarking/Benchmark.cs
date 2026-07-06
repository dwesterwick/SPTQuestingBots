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
        public long MemoryAllocated => EndingMemory > 0 ? EndingMemory - StartingMemory : long.MinValue;
        
        public Benchmark(MethodBase method)
        {
            Method = method;
        }

        public void Start()
        {
            Singleton<LoggingUtil>.Instance.LogDebug($"Running benchmark for {Method.Name}...");

            Status = BenchmarkStatus.Running;

            _benchmarkStopwatch.Restart();
            StartingMemory = BenchmarkService.GetCurrentlyAllocatedMemory();
        }

        public void Stop()
        {
            Stop_Internal();
            Status = BenchmarkStatus.Complete;

            Singleton<LoggingUtil>.Instance.LogDebug($"Running benchmark for {Method.Name}...done.");
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

            Singleton<LoggingUtil>.Instance.LogWarning($"Running benchmark for {Method.Name}...aborted!");
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
            double elapsedTime = _benchmarkStopwatch.ElapsedMillisecondsAsDouble();

            BenchmarkResult result = new BenchmarkResult(overallElapsedTime, Method, elapsedTime, MemoryAllocated);
            Singleton<LoggingUtil>.Instance.LogDebug($"Result for {Method.Name}: Time = {elapsedTime} ms. Allocation = {MemoryAllocated} bytes");

            return result;
        }
    }
}
