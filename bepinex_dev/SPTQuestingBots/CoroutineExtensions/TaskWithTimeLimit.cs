using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.CoroutineExtensions
{
    internal class TaskWithTimeLimit : MethodWithTimeLimit
    {
        public bool IgnoredErrors { get; protected set; } = false;

        protected virtual Task task
        {
            get { return _task; }
            set { _task = value; }
        }

        protected CancellationTokenSource cancellationTokenSource;
        private Task _task;

        public TaskWithTimeLimit(double _maxTimePerIteration) : base(_maxTimePerIteration)
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        public static void WaitForCondition(Func<bool> conditionCheck)
        {
            TaskWithTimeLimit waitForConditionTask = new TaskWithTimeLimit(1);
            waitForConditionTask.Start(() =>
            {
                while (!conditionCheck()) { Thread.Sleep(1); }
            });
            waitForConditionTask.WaitUntilTaskIsComplete();
        }

        public void Start(Action action)
        {
            if (base.IsRunning)
            {
                throw new InvalidOperationException("There is already a task running.");
            }

            SetMethodName(action.Method.Name);
            _task = Task.Run(action, cancellationTokenSource.Token);
            base.cycleTimer.Restart();
            base.jobTimer.Restart();
            base.IsRunning = true;
        }

        public void StartAndIgnoreErrors(Action action)
        {
            SetMethodName(action.Method.Name);
            Action actionWrapper = () =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    IgnoreError(ex);
                }
            };

            this.Start(actionWrapper);
        }

        public IEnumerator WaitForTask()
        {
            if (task == null)
            {
                throw new InvalidOperationException("The task has not been started");
            }

            while (base.IsRunning && taskIsRunning())
            {
                if (task.Wait(1, cancellationTokenSource.Token))
                {
                    break;
                }

                if (stopRequested)
                {
                    base.hadToWait = false;
                    base.IsRunning = false;
                    yield break;
                }

                if (base.cycleTimer.ElapsedMilliseconds > base.maxTimePerIteration)
                {
                    yield return base.WaitForNextFrame();
                }
            }

            base.FinishedWaitingForFrames();
            base.IsRunning = false;
            base.IsCompleted = true;
        }

        public void WaitUntilTaskIsComplete()
        {
            Task waitforTaskToCompleteTask = new Task(() => {
                try
                {
                    while (WaitForTask().MoveNext()) { }
                }
                catch (InvalidOperationException) { }
            });
            waitforTaskToCompleteTask.Start();

            if (!waitforTaskToCompleteTask.Wait(3000))
            {
                LoggingController.LogError("The task for " + MethodName + " timed out.");
            }
            else
            {
                //LoggingController.LogInfo("The task for " + MethodName + " is complete.");
            }
        }

        public void Abort()
        {
            base.stopRequested = true;
            if (!task.IsCompleted)
            {
                cancellationTokenSource.Cancel();
                base.AbortWaitingForFrames();
                base.IsRunning = false;
            }
        }

        protected bool taskIsRunning()
        {
            if (!task.IsCompleted)
            {
                return true;
            }

            return false;
        }

        protected void IgnoreError(Exception ex)
        {
            IgnoredErrors = true;
            LoggingController.LogError(ex.ToString());
            LoggingController.LogWarning("The error above was ignored on purpose.");
        }
    }
}
