using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.CoroutineExtensions
{
    internal class EnumeratorWithTimeLimit : MethodWithTimeLimit
    {
        public EnumeratorWithTimeLimit(double _maxTimePerIteration) : base(_maxTimePerIteration)
        {
            
        }

        public void Reset()
        {
            if (base.IsRunning)
            {
                throw new InvalidOperationException("The iterator is still running");
            }

            base.IsCompleted = false;
            base.hadToWait = false;
            base.stopRequested = false;
            base.cycleTimes.Clear();
            SetMethodName("");
        }

        public IEnumerator Run<TItem>(IEnumerable<TItem> collection, Action<TItem> collectionItemAction)
        {
            SetMethodName(collectionItemAction.Method.Name);
            Action<TItem> action = (item) => { collectionItemAction(item); };
            yield return Run_Internal(collection, action);
        }

        public IEnumerator Run<TItem, T1>(IEnumerable<TItem> collection, Action<TItem, T1> collectionItemAction, T1 param1)
        {
            SetMethodName(collectionItemAction.Method.Name);
            Action<TItem> action = (item) => { collectionItemAction(item, param1); };
            yield return Run_Internal(collection, action);
        }

        public IEnumerator Run<TItem, T1, T2>(IEnumerable<TItem> collection, Action<TItem, T1, T2> collectionItemAction, T1 param1, T2 param2)
        {
            SetMethodName(collectionItemAction.Method.Name);
            Action<TItem> action = (item) => { collectionItemAction(item, param1, param2); };
            yield return Run_Internal(collection, action);
        }

        public IEnumerator Repeat(int repetitions, System.Action action)
        {
            SetMethodName(action.Method.Name);
            yield return Repeat_Internal(repetitions, action);
        }

        public IEnumerator Repeat<T1>(int repetitions, Action<T1> action, T1 param1)
        {
            SetMethodName(action.Method.Name);
            System.Action actionInternal = () => { action(param1); };
            yield return Repeat_Internal(repetitions, actionInternal);
        }

        public IEnumerator Repeat<T1, T2>(int repetitions, Action<T1, T2> action, T1 param1, T2 param2)
        {
            SetMethodName(action.Method.Name);
            System.Action actionInternal = () => { action(param1, param2); };
            yield return Repeat_Internal(repetitions, actionInternal);
        }

        public IEnumerator WaitForCondition(Func<bool> conditionCheck, string conditionName, long timeout)
        {
            SetMethodName(conditionName);
            Run_Internal_Init();

            while (jobTimer.ElapsedMilliseconds < timeout)
            {
                try
                {
                    if (conditionCheck())
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    LoggingController.LogError("Cannot perform condition check for \"" + this.MethodName + "\".");
                    LoggingController.LogError(ex.ToString());
                }

                yield return base.WaitForNextFrame(false);
            }

            try
            {
                if (!conditionCheck())
                {
                    throw new TimeoutException("Condition check for \"" + this.MethodName + "\" was not successful within " + timeout + "ms.");
                }
            }
            catch (Exception ex)
            {
                LoggingController.LogError(ex.ToString());
            }

            Run_Internal_End(false);
        }

        private IEnumerator Run_Internal<TItem>(IEnumerable<TItem> collection, Action<TItem> action)
        {
            Run_Internal_Init();

            foreach (TItem item in collection)
            {
                if (base.stopRequested)
                {
                    base.IsRunning = false;
                    yield break;
                }

                try
                {
                    action(item);
                }
                catch (Exception ex)
                {
                    LoggingController.LogError("Aborting coroutine iteration for " + item.ToString());
                    LoggingController.LogError(ex.ToString());
                }

                if (base.cycleTimer.ElapsedMilliseconds > base.maxTimePerIteration)
                {
                    yield return base.WaitForNextFrame();
                }
            }

            Run_Internal_End();
        }

        private IEnumerator Repeat_Internal(int repetitions, System.Action action)
        {
            Run_Internal_Init();

            for (int repetition = 0; repetition < repetitions; repetition++)
            {
                if (base.stopRequested)
                {
                    base.IsRunning = false;
                    yield break;
                }

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    LoggingController.LogError("Aborting coroutine iteration #" + repetition.ToString());
                    LoggingController.LogError(ex.ToString());
                }

                if (base.cycleTimer.ElapsedMilliseconds > base.maxTimePerIteration)
                {
                    yield return base.WaitForNextFrame();
                }
            }

            Run_Internal_End();
        }

        private void Run_Internal_Init()
        {
            if (base.IsRunning)
            {
                throw new InvalidOperationException("There is already a coroutine running.");
            }

            base.IsCompleted = false;
            base.IsRunning = true;
            base.hadToWait = false;

            base.cycleTimer.Restart();
            base.jobTimer.Restart();
        }

        private void Run_Internal_End(bool writeConsoleMessage = true)
        {
            base.IsRunning = false;
            base.IsCompleted = true;

            base.FinishedWaitingForFrames(writeConsoleMessage);
        }

        public void Abort()
        {
            stopRequested = true;
            base.AbortWaitingForFrames();
        }
    }
}
