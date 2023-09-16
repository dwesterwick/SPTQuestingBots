using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.CoroutineExtensions
{
    internal abstract class MethodWithTimeLimit
    {
        public string MethodName { get; private set; } = "";
        public bool IsRunning { get; protected set; } = false;
        public bool IsCompleted { get; protected set; } = false;

        protected List<long> cycleTimes = new List<long>();
        protected Stopwatch cycleTimer = new Stopwatch();
        protected Stopwatch jobTimer = new Stopwatch();
        protected double maxTimePerIteration;
        protected bool stopRequested = false;
        protected bool hadToWait = false;

        protected MethodWithTimeLimit(double _maxTimePerIteration)
        {
            maxTimePerIteration = _maxTimePerIteration;
        }

        protected void SetMethodName(string _methodName)
        {
            if (MethodName == "")
            {
                MethodName = _methodName;
            }

            if (_methodName == "")
            {
                MethodName = "";
            }
        }

        protected IEnumerator WaitForNextFrame(bool writeConsoleMessage = true, string extraDetail = "")
        {
            cycleTimes.Add(cycleTimer.ElapsedMilliseconds);
            if (writeConsoleMessage && !hadToWait)
            {
                LoggingController.LogWarning(messageTextPrefix(extraDetail) + messageTextSuffix(), true);
            }
            hadToWait = true;
            
            yield return null;
            cycleTimer.Restart();
        }

        protected void FinishedWaitingForFrames(bool writeConsoleMessage = true, string extraDetail = "")
        {
            cycleTimes.Add(cycleTimer.ElapsedMilliseconds);
            if (writeConsoleMessage && hadToWait)
            {
                LoggingController.LogWarning(messageTextPrefix(extraDetail) + "done." + messageTextSuffix(), true);
            }
        }

        protected void AbortWaitingForFrames(string extraDetail = "")
        {
            cycleTimes.Add(cycleTimer.ElapsedMilliseconds);
            if (IsRunning)
            {
                LoggingController.LogWarning(messageTextPrefix(extraDetail) + "aborted." + messageTextSuffix(), true);
            }
        }

        private string messageTextPrefix(string extraDetail = "")
        {
            string message = "Waiting ";
            if (MethodName.Length > 0)
            {
                message += "for " + MethodName + " ";

                if (extraDetail.Length > 0)
                {
                    message += "(" + extraDetail + ") ";
                }
            }
            message += "until next frame...";

            return message;
        }

        private string messageTextSuffix()
        {
            return " (Cycle times: " + string.Join(", ", cycleTimes) + ", Total time: " + jobTimer.ElapsedMilliseconds + ")";
        }
    }
}
