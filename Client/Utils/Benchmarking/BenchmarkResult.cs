using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace QuestingBots.Utils.Benchmarking
{
    public class BenchmarkResult
    {
        public double OverallTime { get; private set; }
        public MethodBase Method { get; private set; }
        public long StartingMemory { get; private set; }
        public long EndingMemory { get; private set; }

        private Stopwatch _benchmarkTimer;
        private double _elapsedMilliseconds = long.MinValue;

        public long MemoryAllocated => EndingMemory > 0 ? EndingMemory - StartingMemory : long.MinValue;

        public BenchmarkResult(double overallTime, MethodBase method, Stopwatch benchmarkTimer, long startingMemory, long endingMemory)
        {
            OverallTime = overallTime;
            Method = method;
            _benchmarkTimer = benchmarkTimer;
            StartingMemory = startingMemory;
            EndingMemory = endingMemory;
        }

        public double GetElapsedMilliseconds()
        {
            if (_elapsedMilliseconds < 0)
            {
                _elapsedMilliseconds = _benchmarkTimer.ElapsedMillisecondsAsDouble();
            }

            return _elapsedMilliseconds;
        }

        public string GetMethodName() => $"{Method.DeclaringType.FullName}.{Method.Name}";
    }
}
