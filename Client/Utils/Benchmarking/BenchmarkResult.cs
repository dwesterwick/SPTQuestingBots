using System;
using System.Collections.Generic;
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

        public string GetMethodName() => $"{Method.DeclaringType.FullName}.{Method.Name}";
    }
}
